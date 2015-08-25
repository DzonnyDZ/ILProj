.namespace $safeprojectname$
{
    .class Program
    {
        .method static void Main(string[] args)
        {
            .entrypoint
            .maxstack 4
            ldstr "Hello World!"
            call void [mscorlib]System.Console::WriteLine(string)
            ldstr "Press any key to continue..."
            call void [mscorlib]System.Console::WriteLine(string)
            call valuetype [mscorlib]System.ConsoleKeyInfo [mscorlib]System.Console::ReadKey()
            pop 
            ret 
        }
    }
}