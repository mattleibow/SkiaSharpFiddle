using ICSharpCode.AvalonEdit;
using System.Windows;
using System;
using Microsoft.Xaml.Behaviors;

namespace SkiaSharpFiddle
{
    public sealed class AvalonEditBehaviour : Behavior<TextEditor>
    {
        private string m_defaultText = @"

in fragmentProcessor color_map;

uniform float3 iResolution;
uniform float  iTime;

half4 main(float2 fragCoord) {

    // Calculate rotation angle based on time (iTime)
    float timePerSecond = iTime / 10;
    float angleSeconds = timePerSecond;
    float angleMinutes = timePerSecond / 60;

    fragCoord.x -= 128;
    fragCoord.y -= 128;
    
    // Displace each row by up to 4 pixels
    fragCoord.x += sin(fragCoord.y / 3) * 4;
    float2 scale = iResolution.xy / iResolution.xy;

    // Get the pixel color from the input image
    half4 color = sample(color_map, fragCoord * scale);

    // Create a rotation matrix
    mat2 secondsRotationMatrix = mat2(cos(angleSeconds), -sin(angleSeconds), sin(angleSeconds), cos(angleSeconds));
    mat2 minutesRotationMatrix = mat2(cos(angleMinutes), -sin(angleMinutes), sin(angleMinutes), cos(angleMinutes));

    // Apply rotation to the coordinates
    vec2 rotatedCoordsSeconds = secondsRotationMatrix * fragCoord.xy;
    vec2 rotatedCoordsMinutes = minutesRotationMatrix * fragCoord.xy;

    // Sample the color from the rotated coordinates
    half4 colorSeconds = sample(color_map, rotatedCoordsSeconds);

    // Sample the color from the rotated coordinates
    half4 colorMinutes = sample(color_map, rotatedCoordsMinutes);

    // Output the final color
    return colorSeconds.bgra + colorMinutes.rgba;
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
