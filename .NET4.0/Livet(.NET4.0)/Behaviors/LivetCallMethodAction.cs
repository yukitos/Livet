using System.Windows.Interactivity;
using System.Windows;
using System.Collections.Generic;

namespace Livet.Behaviors
{
    /// <summary>
    /// 引数を一つだけ持つメソッドに対応したCallMethodActionです。
    /// </summary>
    public class LivetCallMethodAction : TriggerAction<DependencyObject>
    {
        private MethodBinder _method = new MethodBinder();
        private MethodBinderWithArguments _callbackMethod = new MethodBinderWithArguments();

        private bool _parameterSet;

        /// <summary>
        /// メソッドを呼び出すオブジェクトを指定、または取得します。
        /// </summary>
        public object MethodTarget
        {
            get { return GetValue(MethodTargetProperty); }
            set { SetValue(MethodTargetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MethodTarget.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MethodTargetProperty =
            DependencyProperty.Register("MethodTarget", typeof(object), typeof(LivetCallMethodAction), new PropertyMetadata(null));

        /// <summary>
        /// 呼び出すメソッドの名前を指定、または取得します。
        /// </summary>
        public string MethodName
        {
            get { return (string)GetValue(MethodNameProperty); }
            set { SetValue(MethodNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MethodName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MethodNameProperty =
            DependencyProperty.Register("MethodName", typeof(string), typeof(LivetCallMethodAction), new PropertyMetadata(null));


        /// <summary>
        /// 呼び出すメソッドに渡す引数を指定、または取得します。
        /// </summary>
        public object MethodParameter
        {
            get { return GetValue(MethodParameterProperty); }
            set { SetValue(MethodParameterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MethodParameter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MethodParameterProperty =
            DependencyProperty.Register("MethodParameter", typeof(object), typeof(LivetCallMethodAction), new PropertyMetadata(null,OnMethodParameterChanged));

        private static void OnMethodParameterChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var thisReference = (LivetCallMethodAction)sender;

            thisReference._parameterSet = true;
        }

        private static readonly DependencyPropertyKey MethodParametersPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "MethodParameters",
                typeof(List<object>),
                typeof(LivetCallMethodAction),
                new FrameworkPropertyMetadata(new List<object>(), OnMethodParametersChanged));
        public static readonly DependencyProperty MethodParametersProperty =
            MethodParametersPropertyKey.DependencyProperty;

        /// <summary>
        /// 呼び出すメソッドに渡す引数を指定、または取得します。
        /// </summary>
        public List<object> MethodParameters
        {
            get { return (List<object>)GetValue(MethodParametersProperty); }
            set { SetValue(MethodParametersProperty, value); }
        }

        private static void OnMethodParametersChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var thisReference = (LivetCallMethodAction)sender;
            thisReference._parameterSet = true;
        }

        protected override void Invoke(object parameter)
        {
            if(MethodTarget == null) return;
            if (MethodName == null) return;

            if (!_parameterSet)
            {
                _method.Invoke(MethodTarget, MethodName);
            }
            else
            {
                if (MethodParameters != null && MethodParameters.Count > 0)
                {
                    _callbackMethod.Invoke(MethodTarget, MethodName, MethodParameters.ToArray());
                }
                else
                {
                    _callbackMethod.Invoke(MethodTarget, MethodName, new object[] { MethodParameter });
                }
            }
        }
    }
}
