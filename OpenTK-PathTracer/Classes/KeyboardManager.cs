﻿using OpenTK;
using OpenTK.Input;

namespace OpenTK_PathTracer
{
    static class KeyboardManager
    {
        private static KeyboardState lastKeyboardState;
        private static KeyboardState thisKeyboardState;
        public static void Update(KeyboardState keyboardState)
        {
            lastKeyboardState = thisKeyboardState;
            thisKeyboardState = keyboardState;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if key is down this update but not last one</returns>
        public static bool IsKeyTouched(Key key) => thisKeyboardState.IsKeyDown(key) && lastKeyboardState.IsKeyUp(key);

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if key is down</returns>
        public static bool IsKeyDown(Key key) => thisKeyboardState.IsKeyDown(key);

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if key is up this update but not last one</returns>
        public static bool IsKeyUp(Key key) => thisKeyboardState.IsKeyUp(key) && lastKeyboardState.IsKeyDown(key);
    }
}
