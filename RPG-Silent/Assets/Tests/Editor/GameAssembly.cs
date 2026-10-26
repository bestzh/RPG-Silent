using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace RPGSilent.Tests
{
    // The game currently uses Assembly-CSharp. Test asmdefs cannot reference a
    // predefined assembly, so exercise its real compiled types through reflection.
    // This keeps validation from forcing an unrelated runtime assembly migration.
    internal static class GameAssembly
    {
        public static Type Type(string name) => System.Type.GetType(name + ", Assembly-CSharp", true);
        public static object New(string name, params object[] args) => Activator.CreateInstance(Type(name), args);
        public static T Property<T>(object target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);
        public static T Field<T>(object target, string name) => (T)target.GetType().GetField(name).GetValue(target);
        public static void Set(object target, string name, object value) => target.GetType().GetField(name).SetValue(target, value);

        public static object Call(object target, string name, params object[] args)
        {
            try { return target.GetType().GetMethod(name).Invoke(target, args); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }
}
