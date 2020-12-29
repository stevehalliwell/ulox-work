using ULox;
using UnityEngine;
using UnityEngine.Assertions;

public class uLoxBeh : MonoBehaviour
{
    // Start is called before the first frame update
    private void Update()
    {
        Loop_Fib();
    }

    private void Print1_100()
    {
        var engine = new TestLoxEngine();
        var testString =
@"for (var i = 0; i < 100; i = i + 1)
{
    print i;
}";

        var requiredResult =
@"0123456789101112131415161718192021222324252627282930313233343536373839404142434445464748495051525354555657585960616263646566676869707172737475767778798081828384858687888990919293949596979899";

        engine.Run(testString, true);

        Assert.AreEqual(requiredResult, engine.InterpreterResult);
    }

    private void Loop_Fib()
    {
        var engine = new TestLoxEngine();
        var testString =
@"var a = 0;
var temp;

for (var b = 1; a < 10000; b = temp + b)
{
    temp = a;
    a = b;
}

print a;";

        var requiredResult =
@"10946";

        engine.Run(testString, true);

        Assert.AreEqual(requiredResult, engine.InterpreterResult);
    }
}
