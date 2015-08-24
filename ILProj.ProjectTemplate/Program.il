.class private auto ansi beforefieldinit $safeprojectname$.Program extends [mscorlib]System.Object
{
    .method private hidebysig static void Main(string[] args) cil managed
    {
        .entrypoint
        .maxstack 8
        L_0000: nop 
        L_0001: ldstr "Hello World!"
        L_0006: call void [mscorlib]System.Console::WriteLine(string)
        L_000b: nop 
        L_000c: ldstr "Press any key to continue..."
        L_0011: call void [mscorlib]System.Console::WriteLine(string)
        L_0016: nop 
        L_0017: call valuetype [mscorlib]System.ConsoleKeyInfo [mscorlib]System.Console::ReadKey()
        L_001c: pop 
        L_001d: ret 
    }
}