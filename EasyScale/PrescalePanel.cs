using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Lench.EasyScale
{
    public class PrescalePanel : MonoBehaviour
    {
        public bool Minimized { get; private set; } = true;
        public bool Animating { get; private set; } = false;

        public int WindowID { get; } = spaar.ModLoader.Util.GetWindowID();
        public Rect WindowRect = new Rect(500, 500, 240, 50);

        public Vector3 Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    OnScaleChanged?.Invoke(_scale);
                }
            }
        }
        private Vector3 _scale = Vector3.one;

        private string _xSliderString = "1.00";
        private string _ySliderString = "1.00";
        private string _zSliderString = "1.00";
        
        public delegate void ScaleChangeHandler(Vector3 scale);
        public event ScaleChangeHandler OnScaleChanged;

        /// <summary>
        /// To be called after setting slider values externally to update their input fields.
        /// </summary>
        public void RefreshSliderStrings()
        {
            _xSliderString = _scale.x.ToString("0.00");
            _ySliderString = _scale.y.ToString("0.00");
            _zSliderString = _scale.z.ToString("0.00");
        }

        public bool ContainsMouse
        {
            get
            {
                var mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;
                return WindowRect.Contains(mousePos);
            }
        }

        /// <summary>
        /// Unity IMGUI transparent.
        /// </summary>
        public static GUISkin Skin
        {
            get
            {
                var skin = spaar.ModLoader.UI.ModGUI.Skin;
                GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
                skin.window.padding.left = 8;
                skin.window.padding.right = 8;
                skin.window.padding.bottom = 8;
                return skin;
            }
        }

        public Vector2 MinimizedPosition => new Vector2(Screen.width - WindowRect.width, Screen.height - 38);
        public Vector2 Position { get; private set; } = new Vector2(500, 500);

        private static float DrawSlider(string label, float value, float min, float max, string old_text, out string new_text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, spaar.ModLoader.UI.Elements.Labels.Default);
            new_text = Regex.Replace(GUILayout.TextField(old_text,
                spaar.ModLoader.UI.Elements.Labels.Default,
                GUILayout.Width(60)), @"[^0-9\-.]", "");
            GUILayout.EndHorizontal();

            var slider = GUILayout.HorizontalSlider(value, min, max);
            if (new_text != old_text)
            {
                if (new_text != "-" &&
                    !new_text.EndsWith(".") &&
                    !new_text.EndsWith(".0"))
                {
                    float.TryParse(new_text.TrimEnd('-'), out value);
                    new_text = value.ToString("0.00");
                }
            }
            else if (!Equals(slider, value))
            {
                value = slider;
                new_text = value.ToString("0.00");
            }

            return value;
        }

        private void OnGUI()
        {
            if (!spaar.ModLoader.Game.IsSimulating)
            {
                if (Minimized && !Animating) WindowRect.position = MinimizedPosition;

                GUI.skin = Skin;
                WindowRect = GUILayout.Window(WindowID, WindowRect, DoWindow, "", spaar.ModLoader.UI.Elements.Windows.ClearDark,
                    GUILayout.Width(240),
                    GUILayout.Height(10));

                if (!Minimized && !Animating) Position = WindowRect.position;
            }
        }

        private void DoWindow(int id)
        {
            // Draw minimize button
            if (GUILayout.Button("PRESCALE", spaar.ModLoader.UI.Elements.Buttons.Red))
            {
                if (Minimized)
                {
                    Minimized = false;
                    StartCoroutine(Restore());
                }
                else
                {
                    Minimized = true;
                    StartCoroutine(Minimize());
                }
            }
            
            // Draw slider
            var tmpScale = Scale;
            tmpScale.x = DrawSlider("<b>X</b>", tmpScale.x, 0.1f, 3f, _xSliderString, out _xSliderString);
            tmpScale.y = DrawSlider("<b>Y</b>", tmpScale.y, 0.1f, 3f, _ySliderString, out _ySliderString);
            tmpScale.z = DrawSlider("<b>Z</b>", tmpScale.z, 0.1f, 3f, _zSliderString, out _zSliderString);
            Scale = tmpScale;

            // Drag window
            if (!Minimized)
                GUI.DragWindow(new Rect(0, 0, WindowRect.width, WindowRect.height));
        }

        private IEnumerator Restore()
        {
#if DEBUG
            Debug.Log("Showing panel.");
#endif
            Animating = true;
            var pos = WindowRect.position;
            var t = 0f;
            while (t <= 1f)
            {
                if (Minimized) yield break;
                t += Time.deltaTime * 5;
                pos = Vector2.Lerp(pos, Position, t);
                yield return WindowRect.position = pos;
            }
            Animating = false;
        }

        private IEnumerator Minimize()
        {
#if DEBUG
            Debug.Log("Hiding panel.");
#endif
            Animating = true;
            var pos = WindowRect.position;
            var t = 0f;
            while (t <= 1f)
            {
                if (!Minimized) yield break;
                t += Time.deltaTime * 5;
                pos = Vector2.Lerp(pos, MinimizedPosition, t);
                yield return WindowRect.position = pos;
            }
            Animating = false;
        }
    }
}
