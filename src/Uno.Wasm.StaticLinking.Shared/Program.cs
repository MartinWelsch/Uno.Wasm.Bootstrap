﻿// ******************************************************************
// Copyright � 2015-2022 Uno Platform inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.RegularExpressions;
using System.Threading;
using WebAssembly;

namespace Uno.Wasm.Sample
{
	public static class Program
	{
		static void Main()
		{
			// Validate mono tracing with __Native special pinvoke library name
			InternalImportTest.mono_trace_enable(1);

			var runtimeMode = Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_MONO_RUNTIME_MODE");
			Console.WriteLine($"Mono Runtime Mode: " + runtimeMode);

			Console.WriteLine($"test_add:{SideModule1.test_add(21, 21)}");
			Console.WriteLine($"test_float:{SideModule1.test_add_float1(21, 21)}");
			Console.WriteLine($"test_add_double:{SideModule1.test_add_double(21, 21)}");

			var now = DateTime.Now;
			Console.WriteLine($"now:{now} +1:{now.AddDays(1)} -1:{now.AddDays(-1)}");

			var validateEmAddFunctionResult = int.Parse(Runtime.InvokeJS($"Validation.validateEmAddFunction()")) != 0;

			var idbFSValidation = Runtime.InvokeJS($"typeof IDBFS !== 'undefined'");
			Console.WriteLine($"idbFSValidation: {idbFSValidation}");

			var requireAvailable = Runtime.InvokeJS($"typeof require.config !== 'undefined'");
			Console.WriteLine($"requireAvailable: {idbFSValidation}");

			var glAvailable = Runtime.InvokeJS($"typeof GL !== 'undefined'");
			Console.WriteLine($"glAvailable: {glAvailable}");

#if NET7_0_OR_GREATER
			var jsInteropResult = Imports.TestCallback();
#else
			var jsInteropResult = Interop.Runtime.InvokeJS($"testCallback()", out var _);
#endif

			var jsTimeZone = Runtime.InvokeJS($"Intl.DateTimeFormat().resolvedOptions().timeZone");
			var clrTimeZone = TimeZoneInfo.Local.DisplayName;
			var timezoneValidation =
#if NET5_0_OR_GREATER
				true; // Timezone support is not yet enabled for NET 5
#else
				jsTimeZone == clrTimeZone;
#endif

			Console.WriteLine($"Timezone: {jsTimeZone};{clrTimeZone}");

			Console.WriteLine($"SIMD: {Vector.IsHardwareAccelerated}");

#if NET7_0_OR_GREATER
			Console.WriteLine($"Vector64: {Vector64.IsHardwareAccelerated}");
			Console.WriteLine($"Vector128: {Vector128.IsHardwareAccelerated}");
#endif

			File.WriteAllText("/tmp/test.txt", "test.txt");
			var chmodRes = AdditionalImportTest.chmod("/tmp/test.txt", AdditionalImportTest.UGO_RWX);

			var additionalNativeAdd = AdditionalImportTest.additional_native_add(21, 21);

			var res = $"{runtimeMode};" +
				$"{SideModule1.test_add(21, 21)};" +
				$"{SideModule1.test_add_float1(21.1f, 21.2f):.00};" +
				$"{SideModule1.test_add_double(21.3, 21.4)};" +
				$"e{SideModule1.test_exception()};" +
				$"{validateEmAddFunctionResult};" +
				$"{idbFSValidation};" +
				$"{timezoneValidation};" +
				$"{SideModule2.side2_getCustomVersion()};" +
				$"{SideModule3.side3_getCustomVersion()};" +
				$"{SideModule4.side4_getCustomVersion()};" +
				$"{chmodRes};" +
				$"{additionalNativeAdd};" +
				$"requireJs:{requireAvailable};" +
				$"jsInterop:{jsInteropResult};" + 
				$"gl:{glAvailable};"
				;

			var r = Runtime.InvokeJS($"Interop.appendResult(\"{res}\")");

			SideModule1.test_png();
		}
	}

	static class InternalImportTest
	{
		[DllImport("__Native")]
		internal static extern void mono_trace_enable(int enable);
	}

	static class AdditionalImportTest
	{
		public const int UGO_RWX = 0x1ff; // 0777

		[DllImport("libc.so", SetLastError = true)]
		internal static extern int chmod(string pathname, int mode);
		[DllImport("__Native")]
		internal static extern int additional_native_add(int left, int right);
	}

	class SideModule1
	{
		[DllImport("side")]
		internal static extern int test_add(int a, int b);
		[DllImport("side", EntryPoint = "test_add_float")]
		internal static extern float test_add_float1(float a, float b);
		[DllImport("side")]
		internal static extern double test_add_double(double a, double b);
		[DllImport("side")]
		internal static extern int test_exception();
		[DllImport("side")]
		internal static extern void test_png();
	}

	class SideModule2
	{
		[DllImport("side2")]
		internal static extern string side2_getCustomVersion();
	}

	class SideModule3
	{
		[DllImport("side3")]
		internal static extern string side3_getCustomVersion();
	}

	partial class SideModule4
	{
#if NET7_0_OR_GREATER
		
		[LibraryImport("side4", StringMarshalling = StringMarshalling.Utf8)]
		internal static partial string side4_getCustomVersion();
#else
		[DllImport("side4")]
		internal static extern string side4_getCustomVersion();
#endif
	}

#if NET7_0_OR_GREATER
	public static partial class Imports
	{
		[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.testCallback")]
		public static partial string TestCallback();
	}
#endif

	public static partial class Exports
	{
#pragma warning disable IDE0022 // Use expression body for methods (Will be fixed in net7 in RC2)
#if NET7_0_OR_GREATER
		[System.Runtime.InteropServices.JavaScript.JSExport()]
#endif
		public static string MyExportedMethod()
		{
			return $"Invoked";
		}
#pragma warning restore IDE0022 // Use expression body for methods
	}

}
