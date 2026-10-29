using RWCustom;
using LogUtils.Diagnostics;
using LogUtils.Diagnostics.Tests;
using System.Reflection;

namespace TestMod.Tests;

internal static class TestUtils
{
    extension(TestCase testCase)
    {
        public void Fail(string message)
        {
            testCase.AssertThat(false).Fail(new Condition.Message(message));
        }
    }

    extension<T>(Condition<T> cond)
    {
        private static FieldInfo ResultField => TestUtils<T>.ConditionResultField.Value;

        public void Fail(string message)
        {
            cond.Fail(new Condition.Message(message));
        }

        public Condition<T> OnFail(string message)
        {
            if (!cond.Passed)
            {
                Condition.Result result = (Condition.Result)Condition<T>.ResultField.GetValue(cond);
                result.Message = new Condition.Message(message);
            }
            return cond;
        }
    }
}

internal static class TestUtils<T>
{
    internal static Lazy<FieldInfo> ConditionResultField = new(() => typeof(Condition<T>).GetField("Result", BindingFlags.Instance | BindingFlags.NonPublic), false);
}
