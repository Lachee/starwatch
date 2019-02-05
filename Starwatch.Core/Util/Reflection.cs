using System;
using System.Collections.Generic;
using System.Text;

namespace Starwatch.Util
{
    public static class Reflection
    {
        /// <summary>
        /// Alias of <see cref="Type.GetConstructor(Type[])"/> but using params.
        /// </summary>
        /// <param name="type">self type</param>
        /// <param name="parameters">The parameters in the constructor</param>
        /// <returns></returns>
        public static System.Reflection.ConstructorInfo GetConstructor(this Type type, params Type[] parameters) => type.GetConstructor(parameters);

        /// <summary>
        /// Alias of <see cref="System.Reflection.ConstructorInfo.Invoke(object[])"/> but using params.
        /// </summary>
        /// <param name="constructorInfo"></param>
        /// <param name="parameters">The parameters in the constructor</param>
        /// <returns></returns>
        public static object Invoke(this System.Reflection.ConstructorInfo constructorInfo, params object[] parameters) => constructorInfo.Invoke(parameters);
        
    }
}
