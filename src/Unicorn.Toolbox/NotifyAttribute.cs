using AspectInjector.Broker;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Unicorn.Toolbox;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
[Injection(typeof(NotifyAspect))]
public class NotifyAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property)]
[Injection(typeof(NotifyAspect))]
public class NotifyAlsoAttribute : Attribute
{
    public NotifyAlsoAttribute(params string[] notifyAlso) => NotifyAlso = notifyAlso;
    public string[] NotifyAlso { get; }
}

[AttributeUsage(AttributeTargets.Property)]
[Injection(typeof(NotifyAspect))]
public class CallAlsoAttribute : Attribute
{
    public CallAlsoAttribute(params string[] callAlso) => CallAlsoMethod = callAlso;
    public string[] CallAlsoMethod { get; }
}

[Mixin(typeof(INotifyPropertyChanged))]
[Aspect(Scope.Global)]
public class NotifyAspect : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged = (s, e) => { };

    [Advice(Kind.After, Targets = Target.Public | Target.Setter)]
    public void AfterSetter(
        [Argument(Source.Instance)] object source,
        [Argument(Source.Name)] string propName,
        [Argument(Source.Triggers)] Attribute[] triggers
        )
    {
        if (triggers.OfType<NotifyAttribute>().Any())
            PropertyChanged(source, new PropertyChangedEventArgs(propName));

        foreach (var attr in triggers.OfType<NotifyAlsoAttribute>())
            foreach (var additional in attr.NotifyAlso ?? Array.Empty<string>())
                PropertyChanged(source, new PropertyChangedEventArgs(additional));

        foreach (var attr in triggers.OfType<CallAlsoAttribute>())
            foreach (var additional in attr.CallAlsoMethod ?? Array.Empty<string>())
                source.GetType()
                    .GetMethod(additional, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(source, Array.Empty<object>());
    }
}
