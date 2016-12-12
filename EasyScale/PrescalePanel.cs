using System.Collections;
using System.Text.RegularExpressions;
using spaar.ModLoader;
using spaar.ModLoader.UI;
using UnityEngine;

// ReSharper disable UnusedMember.Local

namespace Lench.EasyScale
{
    public class PrescalePanel : MonoBehaviour
    {
        public bool Minimized
        {
            get { return !Mod.PrescaleEnabled; }
            set { Mod.PrescaleEnabled = !value; }
        }

        public bool Animating { get; private set; }

        public int WindowID { get; } = Util.GetWindowID();
        public Rect WindowRect = new Rect(500, 500, 240, 190);

        public Vector3 Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                if (_scale == value) return;
                _scale = value;
                OnScaleChanged?.Invoke(_scale);
            }
        }
        private Vector3 _scale = Vector3.one;

        private string _xSliderString = "1.00";
        private string _ySliderString = "1.00";
        private string _zSliderString = "1.00";
        
        public delegate void ScaleChangeHandler(Vector3 scale);
        public event ScaleChangeHandler OnScaleChanged;

        public delegate void PrescaleToggleHandler(bool value);
        public event PrescaleToggleHandler OnToggle;

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
                var skin = ModGUI.Skin;
                GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
                skin.window.padding.left = 8;
                skin.window.padding.right = 8;
                skin.window.padding.bottom = 8;
                return skin;
            }
        }

        public Vector2 MinimizedPosition => new Vector2(42 - WindowRect.width, Screen.height - 400);
        public static Vector2 Position { get; set; }

        private static float DrawSlider(string label, float value, float min, float max, string oldText, out string newText)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, Elements.Labels.Default);
            newText = Regex.Replace(GUILayout.TextField(oldText,
                Elements.Labels.Default,
                GUILayout.Width(60)), @"[^0-9\-.]", "");
            GUILayout.EndHorizontal();

            var slider = GUILayout.HorizontalSlider(value, min, max);
            if (newText != oldText)
            {
                float.TryParse(newText, out value);
            }
            else if (!Equals(slider, value))
            {
                value = slider;
                newText = value.ToString("0.00");
            }

            return value;
        }

        private void OnGUI()
        {
            if (Game.IsSimulating || !Mod.ModEnabled) return;
            if (!Animating)
                WindowRect.position = Minimized 
                    ? MinimizedPosition 
                    : Position;

            GUI.skin = Skin;
            WindowRect = GUI.Window(WindowID, WindowRect, DoWindow, "", Elements.Windows.ClearDark);

            if (!Minimized && !Animating) Position = WindowRect.position;
        }

        private void DoWindow(int id)
        {
            GUILayout.BeginHorizontal();
            {
                // Draw sliders
                GUILayout.BeginVertical();
                {
                    var tmpScale = Scale;

                    GUILayout.FlexibleSpace();
                    tmpScale.x = DrawSlider("<b>X</b>", tmpScale.x, 0.1f, 3f, _xSliderString, out _xSliderString);
                    GUILayout.FlexibleSpace();
                    tmpScale.y = DrawSlider("<b>Y</b>", tmpScale.y, 0.1f, 3f, _ySliderString, out _ySliderString);
                    GUILayout.FlexibleSpace();
                    tmpScale.z = DrawSlider("<b>Z</b>", tmpScale.z, 0.1f, 3f, _zSliderString, out _zSliderString);
                    GUILayout.FlexibleSpace();

                    Scale = tmpScale;
                }
                GUILayout.EndVertical();

                // Draw minimize button
                GUILayout.BeginVertical(GUILayout.Width(30));
                {
                    if (GUILayout.Button("P\nR\nE\nS\nC\nA\nL\nE",
                        Minimized
                            ? Elements.Buttons.Red
                            : Elements.Buttons.Default))
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
                        OnToggle?.Invoke(!Minimized);
                    }

                    GUILayout.Label(Minimized ? "✘" : "✓", Elements.InputFields.Default);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

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
                t += Time.deltaTime * 3;
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
                t += Time.deltaTime * 3;
                pos = Vector2.Lerp(pos, MinimizedPosition, t);
                yield return WindowRect.position = pos;
            }
            Animating = false;
        }
    }
}
