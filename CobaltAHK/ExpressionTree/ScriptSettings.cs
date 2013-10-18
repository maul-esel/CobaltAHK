using System;

namespace CobaltAHK
{
	public class ScriptSettings
	{
		public ScriptSettings()
		{
			ClipboardTimeout      = 1000;
			ErrorStdOut           = false;
			HotkeyInterval        = 2000;
			HotkeyModifierTimeout = 50;
			IfTimeout             = 1000;
			InstallKeybdHook      = false;
			InstallMouseHook      = false;
			KeyHistory            = 40;
			MaxHotkeysPerInterval = 70;
			MaxThreads            = 10;
			MaxThreadsBuffer      = false;
			MaxThreadsPerHotkey   = 1;
			MustDeclare           = false;
			NoTrayIcon            = false;
			SingleInstance        = SingleInstanceMode.Force;
			WinActivateForce      = false;
		}

		public int ClipboardTimeout { get; set; }

		public bool ErrorStdOut { get; set; }

		public uint HotkeyInterval { get; set; }

		public int HotkeyModifierTimeout { get; set; }

		public uint IfTimeout { get; set; }

		public bool InstallKeybdHook { get; set; }

		public bool InstallMouseHook { get; set; }

		public uint KeyHistory { get; set; } // max: 500

		public uint MaxHotkeysPerInterval { get; set; }

		public uint MaxThreads { get; set; } // max: 255

		public bool MaxThreadsBuffer { get; set; }

		public uint MaxThreadsPerHotkey { get; set; } // max. 20

		public bool MustDeclare { get; set; } // per-file!

		public bool NoTrayIcon { get; set; }

		public SingleInstanceMode SingleInstance { get; set; }

		public bool WinActivateForce { get; set; }

		/* todo:
		Hotstring
		InputLevel (positional)
		MenuMaskKey
		UseHook (positional)
		Warn
		*/

		public enum SingleInstanceMode
		{
			Force,
			Ignore,
			Off
		}
	}
}

