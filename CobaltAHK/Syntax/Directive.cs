using System;

namespace CobaltAHK
{
	public static partial class Syntax
	{
		public enum Directive
		{
			ClipboardTimeout,
			CommentFlag,
			ErrorStdOut,
			EscapeChar,
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
			MaxMem,
			MaxThreads,
			MaxThreadsBuffer,
			MaxThreadsPerHotkey,
			MenuMaskKey,
			NoEnv,
			NoTrayIcon,
			Persistent,
			SingleInstance,
			UseHook,
			Warn,
			WinActivateForce
		}
	}
}

