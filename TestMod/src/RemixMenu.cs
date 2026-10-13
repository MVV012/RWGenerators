using System.Runtime.CompilerServices;
using Menu.Remix.MixedUI;
using RWGenerators;
using UnityEngine;

namespace TestMod;

[GenerateConfigAccessors("Config")]
public class RemixMenu : OptionInterface
{
    public static RemixMenu Instance { get; } = new RemixMenu();

    public static Configurable<bool> Bool { get; } = Bind(true);
    public static Configurable<int> Int { get; } = Bind(0);
    public static Configurable<string> String { get; } = Bind("test");
    public static Configurable<Color> ColorConfig { get; } = Bind(Color.red);
    public static Configurable<CreatureTemplate.Type> ExtEnumConfig { get; } = Bind(CreatureTemplate.Type.BigEel);

    public static Configurable<bool> Field = Bind(false);

    public override void Initialize()
    {
        base.Initialize();

        OpTab tab = new OpTab(this);
        Tabs = [tab];

        float offsetY = 10;
        UIQueue.InitializeQueues(tab, 40, ref offsetY,
            new OpCheckBox.Queue(Bool),
            new OpUpdown.QueueInt(Int),
            new OpTextBox.Queue(String),
            new OpColorPicker.Queue(ColorConfig),
            new OpCheckBox.Queue(Field),
            new OpResourceSelector.QueueEnum(ExtEnumConfig)
        );
    }

    private static Configurable<T> Bind<T>(T defaultValue, string? description = null, [CallerMemberName] string? key = null)
    {
        return Instance.config.Bind(key, defaultValue, new ConfigurableInfo(GetDescription(description, defaultValue)));
    }

    private static Configurable<T> Bind<T>(T defaultValue, T min, T max, string? description = null, [CallerMemberName] string? key = null) where T : IComparable
    {
        return Instance.config.Bind(key, defaultValue, new ConfigurableInfo(GetDescription(description, defaultValue), new ConfigAcceptableRange<T>(min, max)));
    }

    private static string GetDescription<T>(string? description, T defaultValue)
    {
        string defaultString;
        if (defaultValue is bool defaultBool)
        {
            defaultString = defaultBool ? "Enabled" : "Disabled";
        }
        else
        {
            defaultString = defaultValue!.ToString();
        }
        return string.IsNullOrWhiteSpace(description) ? $"Default: {defaultString}" : $"{description}  -  Default: {defaultString}";
    }

    private RemixMenu(): base() {}
}
