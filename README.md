# AutoHotkey on .NET
This is a just for fun and incomplete implementation of AHK on the CLR. Unlike the similar project [IronAHK](https://github.com/polyethene/IronAHK), it uses Microsoft's *Dynamic Language Runtime* (DLR) as base.

## Structure
There are several parts to this:
* an app for execution: see the `CobaltAHK-app` subproject
* the main language implementation (`CobaltAHK` subproject)
	* lexer and parser: implemented here
	* generation of an expression tree: implemented using the DLR, in `CobaltAHK/ExpressionTree/Generator.cs`
	* AHK builtin commands and functions: utilizes IronAHK's library `Rusty.Core` via git submodule

## Prospective
This is a project just for me to learn how lexing / parsing etc. works. It's unlikely to ever become fully usable.

## License
The utilized IronAHK code is provided under the following license:

> Copyright (c) 2010 A. <inspiration3@gmail.com>, Tobias Kappé <tobias@ntlabs.org> and other contributers.
> All rights reserved.
>
> Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
>
>    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
>    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
>
> THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

All other code in this project is licensed [under the MIT license](LICENSE.txt).