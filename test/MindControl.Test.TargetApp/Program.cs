// This application is intended to be used by unit tests as a target to attach to and read expected values.
// It is designed in a way that allows it to be used in various memory manipulation scenarios.
// The basic idea is to create a class instance, wait for a signal of some sort (tbd - input? timing?),
// modify the values of the instance, then wait again for the same kind of signal, and write values in the output.
// This will allow a unit test to both track values, and see if memory manipulation code worked by reading the output.

var outer = new OuterClass();

// Get a pointer to the instance and write it to the console. This will allow the unit test to get a base address to
// test reading methods, and also signal that the instance has been created and initial values are ready to be read.
unsafe
{
    var reference = __makeref(outer);
    IntPtr pointer = **(IntPtr**)(&reference);
    Console.WriteLine($"{pointer:X}");
}

// Wait after creating the instance
Console.In.Peek();

// Modify all values
outer.MyBoolValue = false;
outer.MyByteValue = 0xDC;
outer.MyIntValue = 987411;
outer.MyUintValue = 444763;
outer.MyStringValue = "ThisIsALongerStrîngWith文字化けチェック";
outer.MyLongValue = -777654646516513L;
outer.MyUlongValue = 34411111111164L;
outer.Component.InnerFirstValue = 0xAD;
outer.Component.InnerSecondValue = 64646321;
outer.Component.InnerThirdValue = 7777777777777L;
outer.MyShortValue = -8888;
outer.MyUshortValue = 9999;
outer.MyFloatValue = -123444.147f;
outer.MyDoubleValue = -99879416311.4478;
outer.MyByteArray[0] = 0x55;
outer.MyByteArray[1] = 0x66;
outer.MyByteArray[2] = 0x77;
outer.MyByteArray[3] = 0x88;

// Wait a second time to signal that values have been modified and to give a chance for the tests to modify memory
Console.WriteLine("Waiting before outputting values...");
Console.In.Peek();

// Output final values
Console.WriteLine(outer.MyBoolValue);
Console.WriteLine(outer.MyByteValue);
Console.WriteLine(outer.MyIntValue);
Console.WriteLine(outer.MyUintValue);
Console.WriteLine(outer.MyStringValue);
Console.WriteLine(outer.MyLongValue);
Console.WriteLine(outer.MyUlongValue);
Console.WriteLine(outer.Component.InnerFirstValue);
Console.WriteLine(outer.Component.InnerSecondValue);
Console.WriteLine(outer.Component.InnerThirdValue);
Console.WriteLine(outer.MyShortValue);
Console.WriteLine(outer.MyUshortValue);
Console.WriteLine(outer.MyFloatValue);
Console.WriteLine(outer.MyDoubleValue);

public class OuterClass
{
    public bool MyBoolValue = true;
    public byte MyByteValue = 0xAC;
    public int MyIntValue = -7651;
    public uint MyUintValue = 6781631;
    public string MyStringValue = "ThisIsÄString";
    public long MyLongValue = -65746876815103L;
    public long MyUlongValue = 76354111324644L;
    public readonly InnerClass Component = new();
    public short MyShortValue = -7777;
    public ushort MyUshortValue = 8888;
    public float MyFloatValue = 3456765.323f;
    public double MyDoubleValue = 79879131651.333454;
    public byte[] MyByteArray = { 0x11, 0x22, 0x33, 0x44 };
}

public class InnerClass
{
    public byte InnerFirstValue = 0xAA;
    public int InnerSecondValue = 1111111;
    public long InnerThirdValue = 999999999999L;
}
