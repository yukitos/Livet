using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Livet.Behaviors
{
    /// <summary>
    /// ビヘイビア・トリガー・アクションでのメソッド直接バインディングを可能にするためのクラスです。<br/>
    /// メソッドの実行は最大限キャッシュされます。
    /// </summary>
    public class MethodBinderWithArguments
    {
        private static ConcurrentDictionary<Type, ConcurrentDictionary<string, Action<object, object[]>>> _methodCacheDictionary =
            new ConcurrentDictionary<Type, ConcurrentDictionary<string, Action<object, object[]>>>();

        private string _methodName;

        private Type _targetObjectType;
        private Type[] _argumentTypes;

        private MethodInfo _methodInfo;
        private Action<object,object[]> _method;

        public void Invoke(object targetObject, string methodName, object[] arguments)
        {
            if (targetObject == null) throw new ArgumentNullException("targetObject");
            if (methodName == null) throw new ArgumentNullException("methodName");

            var newTargetObjectType = targetObject.GetType();
            var newArgumentTypes = arguments.Select(i => i.GetType()).ToArray();

            if (_targetObjectType == newTargetObjectType && 
                _methodName == methodName &&
                _argumentTypes.SequenceEqual(newArgumentTypes))
            {
                if (_method != null)
                {
                    _method(targetObject, arguments);
                    return;
                }

                if (TryGetCacheFromMethodCacheDictionary(out _method))
                {
                    _method(targetObject, arguments);
                    return;
                }

                if (_methodInfo != null)
                {
                    _methodInfo.Invoke(targetObject, arguments);
                    return;
                }
            }

            _targetObjectType = newTargetObjectType;
            _argumentTypes = newArgumentTypes;
            _methodName = methodName;

            if (TryGetCacheFromMethodCacheDictionary(out _method))
            {
                _method(targetObject, arguments);
                return;
            }

            var methodParameterTypeError = string.Empty;

            _methodInfo = _targetObjectType.GetMethods()
                .FirstOrDefault(method =>
                {
                    if (method.Name != methodName) return false;

                    var parameters = method.GetParameters();

                    if (_argumentTypes.Length != parameters.Length) return false;

                    for (var i = 0; i < _argumentTypes.Length; i++)
                    {
                        if (!_argumentTypes[i].IsAssignableFrom(parameters[i].ParameterType))
                        {
                            methodParameterTypeError = string.Format(" ({0} 番目の引数に指定された型 '{1}' が型 '{2}' と異なります)",
                                i + 1, _argumentTypes[i], parameters[i].ParameterType);
                            return false;
                        }
                    }

                    return method.ReturnType == typeof(void);
                });

            if (_methodInfo == null)
            {
                throw new ArgumentException(string.Format(
                    "{0} 型に {1} 個の引数を持つメソッド {2} が見つかりません。{3}",
                    _targetObjectType.Name,
                    _argumentTypes.Length,
                    methodName,
                    methodParameterTypeError));
            }

            _methodInfo.Invoke(targetObject, arguments);

            var taskArgument = new Tuple<Type, MethodInfo, Type[]>(_targetObjectType, _methodInfo, _argumentTypes);

            Task.Factory.StartNew(arg =>
            {
                var taskArg = (Tuple<Type, MethodInfo, Type[]>)arg;

                var target = Expression.Parameter(typeof(object), "target");
                var args = Expression.Parameter(typeof(object[]), "args");

                var instance = Expression.Convert(target, taskArg.Item1);
                var methodInfo = taskArg.Item2;
                var methodParams = taskArg.Item3.Select((t, i) => Expression.Convert(Expression.ArrayIndex(args, Expression.Constant(i)), t)).ToArray();
                var methodBody = Expression.Call(instance, methodInfo, methodParams);

                var method = Expression.Lambda<Action<object, object[]>>(methodBody, target, args).Compile();

                var dic = _methodCacheDictionary.GetOrAdd(taskArg.Item1, _ => new ConcurrentDictionary<string, Action<object, object[]>>());
                dic.TryAdd(taskArg.Item2.Name, method);
            }, taskArgument);
        }

        private bool TryGetCacheFromMethodCacheDictionary(out Action<object,object[]> m)
        {
            m = null;
            var foundAction = false;
            ConcurrentDictionary<string, Action<object, object[]>> actionDictionary;
            if (_methodCacheDictionary.TryGetValue(_targetObjectType, out actionDictionary))
            {
                foundAction = actionDictionary.TryGetValue(_methodName, out m);
            }
            return foundAction;
        }
    }
}
