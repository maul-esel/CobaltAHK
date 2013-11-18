; truth values:
; ==========================================================
Assert(""    ? false : true, "Empty string is true")
Assert(0     ? false : true, "0 is true")
Assert(0.0   ? false : true, "0.0 is true")
Assert(false ? false : true, "false is true")
Assert(null  ? false : true, "null is true")
Assert(a     ? false : true, "Uninitialized var is not false")

Assert("a", "Non-empty string is false")
Assert(1, "1 is false")
Assert(true, "true is false")
Assert(!false, "!false is false")
; ==========================================================

Assert(0x2 "" == "2", "Hex-Int to string conversion failed")

five() {
	return(5)
}

Assert(five() "" == "5", "Return value failed")
Assert(5 ** 2 == 25.0, "Power failed")

cond() {
	if (true) {
		return(true)
	}
}
Assert(cond(), "function return in if failed")

glob := "var"
local_func() {
	Assert(local == null, "Uninit var in func was not null")
	local := true
	Assert(local, "Initialized local var in func failed")

	Assert(glob == null, "Global var was visible in function")
}
local_func()

c := 5
c += 8.2
Assert(c == 13.2, "+= failed")

; just testing if the expression isn't too long
b := 5
b .= ": " 5 + 8.2 " = " c " = " . 13.2 " != " 1 * 1 - 13.2 + 5.8 " + 4e-2 == " 0.001 * (0 + 1) * 10 ** 0 " += c"

var := ""
DriveGet(var, "List", "")
Assert(var != "", "'DriveGet List' failed")

Assert(A_YYYY "" == 2013 "", "The year has changed since 2013 :)")

Assert(A_WorkingDir != "", "A_WorkingDir is empty")

WD := A_WorkingDir
Assert(WD == A_WorkingDir, "Failed to set var to builtin var")

A_WorkingDir := A_WorkingDir . "/Tests"
Assert(A_WorkingDir == WD . "/Tests", "Failed to set A_WorkingDir")

Assert((5 < 3) ? false : true, "< failed")
Assert((5 < 3) "" == "0", "Bools as strings")

Assert((5 > 3) * 4 == 4, "Bool as int failed")

obj := { "a" : 4, 4 : 5, 5 : "a" }
Assert(obj != null, "Object literal failed")
Assert(obj.a "" == 4 "", "Object literal incorrect")

Assert(obj.b == null, "Retrieval of undefined object member failed")

obj.a += 5
Assert(obj.a "" == 9 "", "+= on obj member failed")

obj.a := "a"
Assert(obj.a == "a", "obj member assignment failed")

arr := [2, 4, 9]
Assert(arr != null, "Array literal failed")

; just testing for errors on empty literals
obj := {}
arr := []

a := 0
b := "b"
c := "c"
d := "d"
e := "e"
f := "f"
g := "g"

if (g) {
	Assert(g, "Assertion in if-Block failed")
}

; just a comment - divider

if (false) {
	throw "False is true"
} else {
	Assert(g, "Assertion in else-Block failed")
}

; todo: fix parsing so it works without parentheses
result := (a ? b ? c : d : e ? f : g)
Assert(result == "f", "Chained ternary failed")

a := b .= c
Assert(b == "bc", "chained concat-assign failed")
Assert(a == b, "chained assignment failed")

if (true) {
	return()
}
Assert(false, "conditional return in auto-execute failed")

Assert(cond, msg) {
	if (cond) {
		FileAppend("`tAssert (" cond ") : '" msg "'`n", "*")
	} else {
		throw(msg "")
	}
}