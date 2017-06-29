// ********************************************************************************************************
// Product Name: DotSpatial.Serialization.dll
// Description:  A module that supports common functions like serialization.
// ********************************************************************************************************
// The contents of this file are subject to the MIT License (MIT)
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://dotspatial.codeplex.com/license
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF
// ANY KIND, either expressed or implied. See the License for the specific language governing rights and
// limitations under the License.
//
// The Original Code is from MapWindow.dll version 6.0
//
// The Initial Developer of this Original Code is Ted Dunsford. Created 4/1/2009 9:41:48 AM
//
// Contributor(s): (Open source contributors should list themselves and their modifications here).
// |-----------------|---------|---------------------------------------------------------------------
// |      Name       |  Date   |                        Comments
// |-----------------|---------|----------------------------------------------------------------------
//
// ********************************************************************************************************

using System;

namespace DotSpatial
{
    /// <summary>
    /// CloneableEM
    /// </summary>
    public static class CloneableEM
    {
        /// <summary>
        /// The type parameter T is optional, so the intended use would be like:
        /// ObjectType copy = myObject.Copy();
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <param name="original">The original object</param>
        /// <returns>A new object of the same type as the type being copied.</returns>
        public static T Copy<T>(this T original) where T : class, ICloneable
        {
            if (original != null) return original.Clone() as T;
            return null;
        }
    }
}