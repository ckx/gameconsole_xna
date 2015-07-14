## XNA Game Console for XNA 4.0

This is a port of [XNA Game Console](https://console.codeplex.com/) by [vos](https://www.codeplex.com/site/users/view/vos) - it has been modified to work with XNA 4.0. Some additional changes have been made as well, which can be found in the changelog below.

### Changelog

* **XNA 4.0 support**
* **OS Input** - Uses Win32 keyboard input rather than XNA KeyboardState input. This gives a number of advantages, including:
   * Buffered input
   * Paste
   * Auto-detect keyboard layout (I removed the KeyMap class from the original source due to it no longer being necessary)
   * IME-enabled
* **Tab Complete** - You can now autocomplete to commands by hitting tab
* **GameConsole.ExecManual(string command)** - Public function that executes `man` on the string argument
   * Mainly useful for quickly executing `man` after an invalid argument is passed, or in an exception
* **Other Stuff**
   * Traverse buffer with mousewheel 
   * All commands sent to the console are added to inputHistory, even those resulting in an error
     * Handy so if you typo a command you can correct it by hitting up-arrow
   * Prompt character (`Prefix`) added to console log when commands are executed, or when no input is supplied
   * `save` now saves to file "con_log" if no arguments are supplied
   * Some of the built-in commands have been renamed
     * `info` → `con_info`
     * `set` → `con_set`
     * `toggle` → `con_tog`
	 * `help` → removed from commands


##### Note
The original console had some very nice looking fade-in/fade-out capabilities, triggered on the GameConsoleAnimation.Fade state. Two floats that some of the fade logic relied on, `backgroundAlpha` and `textAlpha`, have been commented out in the code. Alpha values are included in the Color struct starting with XNA4 (as a byte), and I keep animation disabled, so I didn't fix it.
