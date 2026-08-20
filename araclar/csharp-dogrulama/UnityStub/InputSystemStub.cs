// Amorland — Unity Input System yer tutuculari (SADECE DERLEME DOGRULAMASI ICIN)
//
// Yalnizca ENABLE_INPUT_SYSTEM tanimliyken derlenir. Amaci: Girdi.cs'in YENI girdi
// sistemi kolunu da gercekten derleyip dogrulamak. Eski kol zaten varsayilan derlemede
// dogrulaniyor; iki kol da yesil olmadan "Unity'de derlenir" denemez.

#if ENABLE_INPUT_SYSTEM
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.InputSystem
{
    public enum TouchPhase { None, Began, Moved, Ended, Canceled, Stationary }

    public class InputControl { public string name { get; set; } }

    public class InputControl<T> : InputControl { public T ReadValue() => default; }

    public class ButtonControl : InputControl<float>
    {
        public bool isPressed => false;
        public bool wasPressedThisFrame => false;
        public bool wasReleasedThisFrame => false;
    }

    public class IntegerControl : InputControl<int> { }
    public class Vector2Control : InputControl<Vector2> { }
    public class TouchPhaseControl : InputControl<TouchPhase> { }

    public class InputDevice { public static InputDevice current => null; }

    public class Pointer : InputDevice
    {
        public Vector2Control position { get; set; }
        public ButtonControl press { get; set; }
    }

    public class Mouse : Pointer
    {
        public static Mouse current => null;
        public ButtonControl leftButton { get; set; }
        public ButtonControl rightButton { get; set; }
        public Vector2Control scroll { get; set; }
    }

    public class Keyboard : InputDevice
    {
        public static Keyboard current => null;
        public ButtonControl escapeKey { get; set; }
        public ButtonControl spaceKey { get; set; }
    }

    public class Touchscreen : Pointer
    {
        public static Touchscreen current => null;
        public IReadOnlyList<Controls.TouchControl> touches => new List<Controls.TouchControl>();
        public Controls.TouchControl primaryTouch { get; set; }
    }
}

namespace UnityEngine.InputSystem.Controls
{
    using UnityEngine.InputSystem;

    public class TouchControl : InputControl
    {
        public IntegerControl touchId { get; set; }
        public Vector2Control position { get; set; }
        public Vector2Control delta { get; set; }
        public TouchPhaseControl phase { get; set; }
        public ButtonControl press { get; set; }
    }
}
#endif
