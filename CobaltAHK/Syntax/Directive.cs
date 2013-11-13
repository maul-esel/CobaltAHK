using System;

namespace CobaltAHK
{
	public static partial class Syntax
	{
		public enum Directive
		{
			ClipboardTimeout,
			ErrorStdOut,
			HotkeyInterval,
			HotkeyModifierTimeout,
			Hotstring,
			If,
			IfTimeout,
			IfWinActive,
			IfWinExist,
			Include,
			InputLevel,
			InstallKeybdHook,
			InstallMouseHook,
			KeyHistory,
			MaxHotkeysPerInterval,
			MaxThreads,
			MaxThreadsBuffer,
			MaxThreadsPerHotkey,
			MenuMaskKey,
			MustDeclare,
			NoTrayIcon,
			SingleInstance,
			UseHook,
			Warn,
			WinActivateForce
		}
	}
}