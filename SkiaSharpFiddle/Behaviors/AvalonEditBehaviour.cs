using ICSharpCode.AvalonEdit;
using System.Windows;
using System;
using Microsoft.Xaml.Behaviors;

namespace SkiaSharpFiddle
{
    public sealed class AvalonEditBehaviour : Behavior<TextEditor>
    {
        private string m_defaultText = @"
uniform shader color_map;

uniform float3 iResolution;
uniform float  iTime;

mat2 rotate2D(float r){
    return mat2(cos(r), sin(r), -sin(r), cos(r));
}

mat3 rotate3D(float angle, vec3 axis){
    vec3 a = normalize(axis);
    float s = sin(angle);
    float c = cos(angle);
    float r = 1.0 - c;
    return mat3(
        a.x * a.x * r + c,
        a.y * a.x * r + a.z * s,
        a.z * a.x * r - a.y * s,
        a.x * a.y * r - a.z * s,
        a.y * a.y * r + c,
        a.z * a.y * r + a.x * s,
        a.x * a.z * r + a.y * s,
        a.y * a.z * r - a.x * s,
        a.z * a.z * r + c
    );
}

half4 main(float2 FC) {
  vec4 o = vec4(0);
  vec2 r = iResolution.xy;
  vec3 v = vec3(1,3,7), p = vec3(0);
  float t=iTime, n=0, e=0, g=0, k=t*.2;
  for (float i=0; i<100; ++i) {
    p = vec3((FC.xy-r*.5)/r.y*g,g)*rotate3D(k,cos(k+v));
    p.z += t;
    p = asin(sin(p)) - 3.;
    n = 0;
    for (float j=0; j<9.; ++j) {
      p.xz *= rotate2D(g/8.);
      p = abs(p);
      p = p.x<p.y ? n++, p.zxy : p.zyx;
      p += p-v;
    }
    g += e = max(p.x,p.z) / 1e3 - .01;
    o.rgb += .1/exp(cos(v*g*.1+n)+3.+1e4*e);
  }
  return o.xyz1;
}";
        public static readonly DependencyProperty TextValueProperty =
            DependencyProperty.Register("TextValue", typeof(string), typeof(AvalonEditBehaviour),
            new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, PropertyChangedCallback));

        public string TextValue
        {
            get { return (string)GetValue(TextValueProperty); }
            set { SetValue(TextValueProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.TextChanged += AssociatedObjectOnTextChanged;
                AssociatedObject.Text = m_defaultText;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
                AssociatedObject.TextChanged -= AssociatedObjectOnTextChanged;
        }

        private void AssociatedObjectOnTextChanged(object sender, EventArgs eventArgs)
        {
            var textEditor = sender as TextEditor;
            if (textEditor != null)
            {
                if (textEditor.Document != null)
                    TextValue = textEditor.Document.Text;
            }
        }

        private static void PropertyChangedCallback(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var behavior = dependencyObject as AvalonEditBehaviour;
            if (behavior.AssociatedObject != null)
            {
                var editor = behavior.AssociatedObject as TextEditor;
                if (editor.Document != null)
                {
                    var caretOffset = editor.CaretOffset;
                    editor.Document.Text = dependencyPropertyChangedEventArgs.NewValue.ToString();
                    editor.CaretOffset = caretOffset;
                }
            }
        }
    }
}
