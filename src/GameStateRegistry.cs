using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WolfCurses.Core;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet
{
    /// <summary>
    ///     Registers the game's reflection-discovered types from the game assembly itself.
    /// </summary>
    /// <remarks>
    ///     WolfCurses 2018.9.12.1 discovers <see cref="ParentWindowAttribute" /> forms (and its
    ///     <c>AttributeExtensions.GetTypesWith</c> helper discovers everything else) by scanning
    ///     <see cref="Assembly.GetEntryAssembly" />. That is only the game when OregonTrailDotNet is the executable. Hosted
    ///     by another program, such as the tests/Backend harness, the form factory is empty and the first
    ///     <c>SetForm</c> throws "State factory cannot create state from type that does not exist in reference states"
    ///     (issue #40), while the event director silently finds no random events.
    /// </remarks>
    internal static class GameStateRegistry
    {
        private static readonly Assembly GameAssembly = typeof(GameStateRegistry).Assembly;

        private static readonly FieldInfo FormFactoryField =
            typeof(WindowManager).GetField("_formFactory", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly PropertyInfo LoadedFormsProperty =
            typeof(FormFactory).GetProperty("LoadedForms", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>Non-generic game types decorated with the attribute, read from the game assembly.</summary>
        internal static IEnumerable<Type> TypesWith<TAttribute>(bool inherit) where TAttribute : Attribute
        {
            return GameAssembly.DefinedTypes
                .Where(type => type.IsDefined(typeof(TAttribute), inherit))
                .Select(type => type.AsType())
                .ToList();
        }

        /// <summary>Adds every game form to the window manager's form factory, keyed by its declared parent window.</summary>
        internal static void RegisterForms(WindowManager windowManager)
        {
            var loadedForms = LoadedForms(windowManager);
            foreach (var form in TypesWith<ParentWindowAttribute>(false))
            {
                var parent = form.GetTypeInfo().GetCustomAttribute<ParentWindowAttribute>(false).ParentWindow;
                loadedForms.TryAdd(form, parent);
            }
        }

        /// <summary>The form types the window manager can currently create.</summary>
        internal static IReadOnlyCollection<Type> RegisteredForms(WindowManager windowManager)
        {
            return LoadedForms(windowManager).Keys.ToList();
        }

        private static Dictionary<Type, Type> LoadedForms(WindowManager windowManager)
        {
            if (windowManager == null) throw new ArgumentNullException(nameof(windowManager));
            // The package version is pinned; fail loudly rather than run a journey that cannot open its screens.
            var factory = FormFactoryField?.GetValue(windowManager) as FormFactory;
            if (factory != null && LoadedFormsProperty?.GetValue(factory) is Dictionary<Type, Type> loadedForms)
                return loadedForms;
            throw new InvalidOperationException(
                "WolfCurses' form factory layout changed; game forms cannot be registered. Re-check GameStateRegistry.");
        }
    }
}
