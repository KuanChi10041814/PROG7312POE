namespace PROG7312POE.Core.Models;

public readonly record struct PowerReading(string Source, double Watts)
{
    public static PowerReading operator +(PowerReading left, PowerReading right)
    {
        return new PowerReading($"{left.Source}+{right.Source}", left.Watts + right.Watts);
    }

    public static PowerReading operator -(PowerReading left, PowerReading right)
    {
        return new PowerReading($"{left.Source}-{right.Source}", left.Watts - right.Watts);
    }

    public static bool operator >(PowerReading left, PowerReading right)
    {
        return left.Watts > right.Watts;
    }

    public static bool operator <(PowerReading left, PowerReading right)
    {
        return left.Watts < right.Watts;
    }
}
