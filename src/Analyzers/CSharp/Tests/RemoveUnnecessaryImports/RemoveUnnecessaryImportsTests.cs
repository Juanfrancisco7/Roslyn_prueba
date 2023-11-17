﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryImports
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpRemoveUnnecessaryImportsDiagnosticAnalyzer,
        CSharpRemoveUnnecessaryImportsCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
    public class RemoveUnnecessaryImportsTests
    {
        [Fact]
        public async Task TestNoReferences()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using System;
                using System.Collections.Generic;
                using System.Linq;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
                """,
                """
                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestNoReferencesWithCopyright()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                // Copyright (c) Somebody.

                [|{|IDE0005:using System;
                using System.Collections.Generic;
                using System.Linq;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
                """,
                """
                // Copyright (c) Somebody.

                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27006")]
        public async Task TestReferencesWithCopyrightAndPreservableTrivia()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                // Copyright (c) Somebody.

                [|using System;

                {|IDE0005:using System.Collections.Generic;
                // This is important
                using System.Linq;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        Action a;
                    }
                }
                """,
                """
                // Copyright (c) Somebody.

                using System;
                // This is important

                class Program
                {
                    static void Main(string[] args)
                    {
                        Action a;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27006")]
        public async Task TestReferencesWithCopyrightAndGroupings()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                // Copyright (c) Somebody.

                [|using System;

                {|IDE0005:using System.Collections.Generic;

                using System.Linq;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        Action a;
                    }
                }
                """,
                """
                // Copyright (c) Somebody.

                using System;

                class Program
                {
                    static void Main(string[] args)
                    {
                        Action a;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestIdentifierReferenceInTypeContext()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|using System;
                {|IDE0005:using System.Collections.Generic;
                using System.Linq;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        DateTime d;
                    }
                }
                """,
                """
                using System;

                class Program
                {
                    static void Main(string[] args)
                    {
                        DateTime d;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestGeneratedCode()
        {
            var source = """
                // <auto-generated/>

                [|{|IDE0005_gen:using System;|}
                using System.Collections.Generic;
                {|IDE0005_gen:using System.Linq;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        List<int> d;
                    }
                }
                """;
            var fixedSource = """
                // <auto-generated/>

                using System.Collections.Generic;

                class Program
                {
                    static void Main(string[] args)
                    {
                        List<int> d;
                    }
                }
                """;

            // Fix All operations in generated code do not apply changes
            var batchFixedSource = source;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                BatchFixedState =
                {
                    Sources = { batchFixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestGenericReferenceInTypeContext()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using System;|}
                using System.Collections.Generic;
                {|IDE0005:using System.Linq;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        List<int> list;
                    }
                }
                """,
                """
                using System.Collections.Generic;

                class Program
                {
                    static void Main(string[] args)
                    {
                        List<int> list;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMultipleReferences()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|using System;
                using System.Collections.Generic;
                {|IDE0005:using System.Linq;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        List<int> list;
                        DateTime d;
                    }
                }
                """,
                """
                using System;
                using System.Collections.Generic;

                class Program
                {
                    static void Main(string[] args)
                    {
                        List<int> list;
                        DateTime d;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestExtensionMethodReference()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using System;
                using System.Collections.Generic;|}
                using System.Linq;|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        args.Where(a => a.Length > 10);
                    }
                }
                """,
                """
                using System.Linq;

                class Program
                {
                    static void Main(string[] args)
                    {
                        args.Where(a => a.Length > 10);
                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541827")]
        public async Task TestExtensionMethodLinq()
        {
            // NOTE: Intentionally not running this test with Script options, because in Script,
            // NOTE: class "Goo" is placed inside the script class, and can't be seen by the extension
            // NOTE: method Select, which is not inside the script class.
            var code = """
                using System;
                using System.Collections;
                using SomeNS;

                class Program
                {
                    static void Main()
                    {
                        Goo qq = new Goo();
                        IEnumerable x = from q in qq
                                        select q;
                    }
                }

                public class Goo
                {
                    public Goo()
                    {
                    }
                }

                namespace SomeNS
                {
                    public static class SomeClass
                    {
                        public static IEnumerable Select(this Goo o, Func<object, object> f)
                        {
                            return null;
                        }
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestAliasQualifiedAliasReference()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using System;|}
                using G = System.Collections.Generic;
                {|IDE0005:using System.Linq;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        G::List<int> list;
                    }
                }
                """,
                """
                using G = System.Collections.Generic;

                class Program
                {
                    static void Main(string[] args)
                    {
                        G::List<int> list;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestQualifiedAliasReference()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using System;|}
                using G = System.Collections.Generic;|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        G.List<int> list;
                    }
                }
                """,
                """
                using G = System.Collections.Generic;

                class Program
                {
                    static void Main(string[] args)
                    {
                        G.List<int> list;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestNestedUnusedUsings()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using System;
                using System.Collections.Generic;
                using System.Linq;|}|]

                namespace N
                {
                    using System;

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            DateTime d;
                        }
                    }
                }
                """,
                """
                namespace N
                {
                    using System;

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            DateTime d;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestNestedUnusedUsings_FileScopedNamespace()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                [|{|IDE0005:using System;
                using System.Collections.Generic;
                using System.Linq;|}|]

                namespace N;

                using System;

                class Program
                {
                    static void Main(string[] args)
                    {
                        DateTime d;
                    }
                }
                """,
                FixedCode =
                """
                namespace N;

                using System;

                class Program
                {
                    static void Main(string[] args)
                    {
                        DateTime d;
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNestedUsedUsings()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|using System;
                {|IDE0005:using System.Collections.Generic;
                using System.Linq;|}|]

                namespace N
                {
                    using System;

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            DateTime d;
                        }
                    }
                }

                class F
                {
                    DateTime d;
                }
                """,
                """
                using System;

                namespace N
                {
                    using System;

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            DateTime d;
                        }
                    }
                }

                class F
                {
                    DateTime d;
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/712656")]
        public async Task TestNestedUsedUsings2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|using System;
                {|IDE0005:using System.Collections.Generic;
                using System.Linq;|}|]

                namespace N
                {
                    [|using System;
                    {|IDE0005:using System.Collections.Generic;|}|]

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            DateTime d;
                        }
                    }
                }

                class F
                {
                    DateTime d;
                }
                """,
                """
                using System;

                namespace N
                {
                    using System;

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            DateTime d;
                        }
                    }
                }

                class F
                {
                    DateTime d;
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/712656")]
        public async Task TestNestedUsedUsings2_FileScopedNamespace()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                [|{|IDE0005:using System;
                using System.Collections.Generic;
                using System.Linq;|}|]

                namespace N;

                [|using System;
                {|IDE0005:using System.Collections.Generic;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        DateTime d;
                    }
                }

                class F
                {
                    DateTime d;
                }
                """,
                FixedCode =
                """
                namespace N;

                using System;

                class Program
                {
                    static void Main(string[] args)
                    {
                        DateTime d;
                    }
                }

                class F
                {
                    DateTime d;
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task TestAttribute()
        {
            var code = """
                using SomeNamespace;

                [SomeAttr]
                class Goo
                {
                }

                namespace SomeNamespace
                {
                    public class SomeAttrAttribute : System.Attribute
                    {
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestAttributeArgument()
        {
            var code = """
                using goo;

                [SomeAttribute(typeof(SomeClass))]
                class Program
                {
                    static void Main()
                    {
                    }
                }

                public class SomeAttribute : System.Attribute
                {
                    public SomeAttribute(object f)
                    {
                    }
                }

                namespace goo
                {
                    public class SomeClass
                    {
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestRemoveAllWithSurroundingPreprocessor()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                #if true

                [|{|IDE0005:using System;
                using System.Collections.Generic;|}|]

                #endif

                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
                """,
                """
                #if true

                #endif

                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRemoveFirstWithSurroundingPreprocessor()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                #if true

                [|{|IDE0005:using System;|}
                using System.Collections.Generic;|]

                #endif

                class Program
                {
                    static void Main(string[] args)
                    {
                        List<int> list;
                    }
                }
                """,
                """
                #if true

                using System.Collections.Generic;

                #endif

                class Program
                {
                    static void Main(string[] args)
                    {
                        List<int> list;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRemoveAllWithSurroundingPreprocessor2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                namespace N
                {
                #if true

                    [|{|IDE0005:using System;
                    using System.Collections.Generic;|}|]

                #endif

                    class Program
                    {
                        static void Main(string[] args)
                        {
                        }
                    }
                }
                """,
                """
                namespace N
                {
                #if true

                #endif

                    class Program
                    {
                        static void Main(string[] args)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRemoveOneWithSurroundingPreprocessor2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                namespace N
                {
                #if true

                    [|{|IDE0005:using System;|}
                    using System.Collections.Generic;|]

                #endif

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            List<int> list;
                        }
                    }
                }
                """,
                """
                namespace N
                {
                #if true

                    using System.Collections.Generic;

                #endif

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            List<int> list;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541817")]
        public async Task TestComments8718()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|using Goo; {|IDE0005:using System.Collections.Generic; /*comment*/|} using Goo2;|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        Bar q;
                        Bar2 qq;
                    }
                }

                namespace Goo
                {
                    public class Bar
                    {
                    }
                }

                namespace Goo2
                {
                    public class Bar2
                    {
                    }
                }
                """,
                """
                using Goo;
                using Goo2;

                class Program
                {
                    static void Main(string[] args)
                    {
                        Bar q;
                        Bar2 qq;
                    }
                }

                namespace Goo
                {
                    public class Bar
                    {
                    }
                }

                namespace Goo2
                {
                    public class Bar2
                    {
                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528609")]
        public async Task TestComments()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                //c1
                /*c2*/
                {|IDE0005:[|using/*c3*/ System/*c4*/;|] //c5|}
                //c6

                class Program
                {
                }
                """,
                """
                //c1
                /*c2*/
                //c6

                class Program
                {
                }
                """);
        }

        [Fact]
        public async Task TestUnusedUsing()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using System.Collections.Generic;|}|]

                class Program
                {
                    static void Main()
                    {
                    }
                }
                """,
                """
                class Program
                {
                    static void Main()
                    {
                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541827")]
        public async Task TestSimpleQuery()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using System;
                using System.Collections.Generic;|}
                using System.Linq;|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        var q = from a in args
                                where a.Length > 21
                                select a;
                    }
                }
                """,
                """
                using System.Linq;

                class Program
                {
                    static void Main(string[] args)
                    {
                        var q = from a in args
                                where a.Length > 21
                                select a;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUsingStaticClassAccessField1()
        {
            // Test intentionally uses 'using' instead of 'using static'
            var testCode = """
                [|{|IDE0005:using {|CS0138:SomeNS.Goo|};|}|]

                class Program
                {
                    static void Main()
                    {
                        var q = {|CS0103:x|};
                    }
                }

                namespace SomeNS
                {
                    static class Goo
                    {
                        public static int x;
                    }
                }
                """;
            var fixedCode = """
                class Program
                {
                    static void Main()
                    {
                        var q = {|CS0103:x|};
                    }
                }

                namespace SomeNS
                {
                    static class Goo
                    {
                        public static int x;
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp5,
            }.RunAsync();
        }

        [Fact]
        public async Task TestUsingStaticClassAccessField2()
        {
            var code = """
                using static SomeNS.Goo;

                class Program
                {
                    static void Main()
                    {
                        var q = x;
                    }
                }

                namespace SomeNS
                {
                    static class Goo
                    {
                        public static int x;
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestUsingStaticClassAccessMethod1()
        {
            // Test intentionally uses 'using' instead of 'using static'
            var testCode = """
                [|{|IDE0005:using {|CS0138:SomeNS.Goo|};|}|]

                class Program
                {
                    static void Main()
                    {
                        var q = {|CS0103:X|}();
                    }
                }

                namespace SomeNS
                {
                    static class Goo
                    {
                        public static int X()
                        {
                            return 42;
                        }
                    }
                }
                """;
            var fixedCode = """
                [|class Program
                {
                    static void Main()
                    {
                        var q = {|CS0103:X|}();
                    }
                }

                namespace SomeNS
                {
                    static class Goo
                    {
                        public static int X()
                        {
                            return 42;
                        }
                    }
                }|]
                """;

            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp5,
            }.RunAsync();
        }

        [Fact]
        public async Task TestUsingStaticClassAccessMethod2()
        {
            var code = """
                using static SomeNS.Goo;

                class Program
                {
                    static void Main()
                    {
                        var q = X();
                    }
                }

                namespace SomeNS
                {
                    static class Goo
                    {
                        public static int X()
                        {
                            return 42;
                        }
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, WorkItem(8846, "DevDiv_Projects/Roslyn")]
        public async Task TestUnusedTypeImportIsRemoved1()
        {
            // Test intentionally uses 'using' instead of 'using static'
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using {|CS0138:SomeNS.Goo|};|}|]

                class Program
                {
                    static void Main()
                    {
                    }
                }

                namespace SomeNS
                {
                    static class Goo
                    {
                    }
                }
                """,
                """
                class Program
                {
                    static void Main()
                    {
                    }
                }

                namespace SomeNS
                {
                    static class Goo
                    {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUnusedTypeImportIsRemoved2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using static SomeNS.Goo;|}|]

                class Program
                {
                    static void Main()
                    {
                    }
                }

                namespace SomeNS
                {
                    static class Goo
                    {
                    }
                }
                """,
                """
                class Program
                {
                    static void Main()
                    {
                    }
                }

                namespace SomeNS
                {
                    static class Goo
                    {
                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541817")]
        public async Task TestRemoveTrailingComment()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                {|IDE0005:[|using System.Collections.Generic;|] // comment|}

                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
                """,
                """
                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541914")]
        public async Task TestRemovingUnbindableUsing()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using {|CS0246:gibberish|};|}|]

                public static class Program
                {
                }
                """,
                """
                public static class Program
                {
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541937")]
        public async Task TestAliasInUse()
        {
            var code = """
                using GIBBERISH = Goo.Bar;

                class Program
                {
                    static void Main(string[] args)
                    {
                        GIBBERISH x;
                    }
                }

                namespace Goo
                {
                    public class Bar
                    {
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541914")]
        public async Task TestRemoveUnboundUsing()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using {|CS0246:gibberish|};|}|]

                public static class Program
                {
                }
                """,
                """
                public static class Program
                {
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542016")]
        public async Task TestLeadingNewlines1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|{|IDE0005:using System;
                using System.Collections.Generic;
                using System.Linq;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {

                    }
                }
                """,
                """
                class Program
                {
                    static void Main(string[] args)
                    {

                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542016")]
        public async Task TestRemoveLeadingNewLines2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                namespace N
                {
                    [|{|IDE0005:using System;
                    using System.Collections.Generic;
                    using System.Linq;|}|]

                    class Program
                    {
                        static void Main(string[] args)
                        {

                        }
                    }
                }
                """,
                """
                namespace N
                {
                    class Program
                    {
                        static void Main(string[] args)
                        {

                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542134")]
        public async Task TestImportedTypeUsedAsGenericTypeArgument()
        {
            var code = """
                using GenericThingie;

                public class GenericType<T>
                {
                }

                namespace GenericThingie
                {
                    public class Something
                    {
                    }
                }

                public class Program
                {
                    void goo()
                    {
                        GenericType<Something> type;
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542723")]
        public async Task TestRemoveCorrectUsing1()
        {
            var source = """
                using System.Collections.Generic;

                namespace Goo
                {
                    [|{|IDE0005:using Bar = Dictionary<string, string>;|}|]
                }
                """;
            var fixedSource = """
                namespace Goo
                {
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,

                // Fixing the first diagnostic introduces a second diagnostic to fix.
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2,
            }.RunAsync();
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542723")]
        public async Task TestRemoveCorrectUsing2()
        {
            var code = """
                using System.Collections.Generic;

                namespace Goo
                {
                    using Bar = Dictionary<string, string>;

                    class C
                    {
                        Bar b;
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestSpan()
        {
            var code = """
                namespace N
                {
                    [|{|IDE0005:using System;|}|]
                }
                """;
            var fixedCode = """
                namespace N
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543000")]
        public async Task TestMissingWhenErrorsWouldBeGenerated()
        {
            var code = """
                using System;
                using X;
                using Y;

                class B
                {
                    static void Main()
                    {
                        Bar(x => x.Goo());
                    }

                    static void Bar(Action<int> x)
                    {
                    }

                    static void Bar(Action<string> x)
                    {
                    }
                }

                namespace X
                {
                    public static class A
                    {
                        public static void Goo(this int x)
                        {
                        }

                        public static void Goo(this string x)
                        {
                        }
                    }
                }

                namespace Y
                {
                    public static class B
                    {
                        public static void Goo(this int x)
                        {
                        }
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544976")]
        public async Task TestMissingWhenMeaningWouldChangeInLambda()
        {
            var code = """
                using System;
                using X;
                using Y;

                class B
                {
                    static void Main()
                    {
                        Bar(x => x.Goo(), null); // Prints 1
                    }

                    static void Bar(Action<string> x, object y)
                    {
                        Console.WriteLine(1);
                    }

                    static void Bar(Action<int> x, string y)
                    {
                        Console.WriteLine(2);
                    }
                }

                namespace X
                {
                    public static class A
                    {
                        public static void Goo(this int x)
                        {
                        }

                        public static void Goo(this string x)
                        {
                        }
                    }
                }

                namespace Y
                {
                    public static class B
                    {
                        public static void Goo(this int x)
                        {
                        }
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544976")]
        public async Task TestCasesWithLambdas1()
        {
            // NOTE: Y is used when speculatively binding "x => x.Goo()".  As such, it is marked as
            // used even though it isn't in the final bind, and could be removed.  However, as we do
            // not know if it was necessary to eliminate a speculative lambda bind, we must leave
            // it.
            var code = """
                using System;
                using X;
                using Y;

                class B
                {
                    static void Main()
                    {
                        Bar(x => x.Goo(), null); // Prints 1
                    }

                    static void Bar(Action<string> x, object y)
                    {
                    }
                }

                namespace X
                {
                    public static class A
                    {
                        public static void Goo(this string x)
                        {
                        }
                    }
                }

                namespace Y
                {
                    public static class B
                    {
                        public static void Goo(this int x)
                        {
                        }
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545646")]
        public async Task TestCasesWithLambdas2()
        {
            var code = """
                using System;
                using N; // Falsely claimed as unnecessary

                static class C
                {
                    static void Ex(this string x)
                    {
                    }

                    static void Inner(Action<string> x, string y)
                    {
                    }

                    static void Inner(Action<string> x, int y)
                    {
                    }

                    static void Inner(Action<int> x, int y)
                    {
                    }

                    static void Outer(Action<string> x, object y)
                    {
                        Console.WriteLine(1);
                    }

                    static void Outer(Action<int> x, string y)
                    {
                        Console.WriteLine(2);
                    }

                    static void Main()
                    {
                        Outer(y => Inner(x => x.Ex(), y), null);
                    }
                }

                namespace N
                {
                    static class E
                    {
                        public static void Ex(this int x)
                        {
                        }
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545741")]
        public async Task TestMissingOnAliasedVar()
        {
            var code = """
                using var = var;

                class var
                {
                }

                class B
                {
                    static void Main()
                    {
                        var a = 1;
                    }
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546115")]
        public async Task TestBrokenCode()
        {
            var code = """
                using System.Linq;

                public class QueryExpressionTest
                {
                    public static void Main()
                    {
                        var expr1 = new[] { };
                        var expr2 = new[] { };
                        var query8 = from int i in expr1
                                     join int fixed in expr2 on i equals fixed select new { i, fixed };

                    var query9 = from object i in expr1
                                 join object fixed in expr2 on i equals fixed select new { i, fixed };
                  }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                ExpectedDiagnostics =
                {
                    // Test0.cs(7,21): error CS0826: No best type found for implicitly-typed array
                    DiagnosticResult.CompilerError("CS0826").WithSpan(7, 21, 7, 30),
                    // Test0.cs(8,21): error CS0826: No best type found for implicitly-typed array
                    DiagnosticResult.CompilerError("CS0826").WithSpan(8, 21, 8, 30),
                    // Test0.cs(10,31): error CS0742: A query body must end with a select clause or a group clause
                    DiagnosticResult.CompilerError("CS0742").WithSpan(10, 31, 10, 36),
                    // Test0.cs(10,31): error CS0743: Expected contextual keyword 'on'
                    DiagnosticResult.CompilerError("CS0743").WithSpan(10, 31, 10, 36),
                    // Test0.cs(10,31): error CS0744: Expected contextual keyword 'equals'
                    DiagnosticResult.CompilerError("CS0744").WithSpan(10, 31, 10, 36),
                    // Test0.cs(10,31): error CS1001: Identifier expected
                    DiagnosticResult.CompilerError("CS1001").WithSpan(10, 31, 10, 36),
                    // Test0.cs(10,31): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(10, 31, 10, 36),
                    // Test0.cs(10,31): error CS1003: Syntax error, 'in' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(10, 31, 10, 36).WithArguments("in"),
                    // Test0.cs(10,31): error CS1525: Invalid expression term 'fixed'
                    DiagnosticResult.CompilerError("CS1525").WithSpan(10, 31, 10, 36).WithArguments("fixed"),
                    // Test0.cs(10,31): error CS1525: Invalid expression term 'fixed'
                    DiagnosticResult.CompilerError("CS1525").WithSpan(10, 31, 10, 36).WithArguments("fixed"),
                    // Test0.cs(10,31): error CS1525: Invalid expression term 'fixed'
                    DiagnosticResult.CompilerError("CS1525").WithSpan(10, 31, 10, 36).WithArguments("fixed"),
                    // Test0.cs(10,31): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    DiagnosticResult.CompilerError("CS0214").WithSpan(10, 31, 10, 49),
                    // Test0.cs(10,37): error CS0209: The type of a local declared in a fixed statement must be a pointer type
                    DiagnosticResult.CompilerError("CS0209").WithSpan(10, 37, 10, 37),
                    // Test0.cs(10,37): error CS0210: You must provide an initializer in a fixed or using statement declaration
                    DiagnosticResult.CompilerError("CS0210").WithSpan(10, 37, 10, 37),
                    // Test0.cs(10,37): error CS1001: Identifier expected
                    DiagnosticResult.CompilerError("CS1001").WithSpan(10, 37, 10, 39),
                    // Test0.cs(10,37): error CS1003: Syntax error, '(' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(10, 37, 10, 39).WithArguments("("),
                    // Test0.cs(10,37): error CS1003: Syntax error, ',' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(10, 37, 10, 39).WithArguments(","),
                    // Test0.cs(10,37): error CS1031: Type expected
                    DiagnosticResult.CompilerError("CS1031").WithSpan(10, 37, 10, 39),
                    // Test0.cs(10,40): error CS0118: 'expr2' is a variable but is used like a type
                    DiagnosticResult.CompilerError("CS0118").WithSpan(10, 40, 10, 45).WithMessage(null),
                    // Test0.cs(10,40): error CS1026: ) expected
                    DiagnosticResult.CompilerError("CS1026").WithSpan(10, 40, 10, 45),
                    // Test0.cs(10,40): error CS1023: Embedded statement cannot be a declaration or labeled statement
                    DiagnosticResult.CompilerError("CS1023").WithSpan(10, 40, 10, 49),
                    // Test0.cs(10,49): error CS0246: The type or namespace name 'i' could not be found (are you missing a using directive or an assembly reference?)
                    DiagnosticResult.CompilerError("CS0246").WithSpan(10, 49, 10, 50).WithArguments("i"),
                    // Test0.cs(10,49): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(10, 49, 10, 50),
                    // Test0.cs(10,58): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(10, 58, 10, 63),
                    // Test0.cs(10,58): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    DiagnosticResult.CompilerError("CS0214").WithSpan(10, 58, 10, 80),
                    // Test0.cs(10,64): error CS0246: The type or namespace name 'select' could not be found (are you missing a using directive or an assembly reference?)
                    DiagnosticResult.CompilerError("CS0246").WithSpan(10, 64, 10, 70).WithArguments("select"),
                    // Test0.cs(10,64): error CS1003: Syntax error, '(' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(10, 64, 10, 70).WithArguments("("),
                    // Test0.cs(10,71): error CS0209: The type of a local declared in a fixed statement must be a pointer type
                    DiagnosticResult.CompilerError("CS0209").WithSpan(10, 71, 10, 71),
                    // Test0.cs(10,71): error CS0210: You must provide an initializer in a fixed or using statement declaration
                    DiagnosticResult.CompilerError("CS0210").WithSpan(10, 71, 10, 71),
                    // Test0.cs(10,71): error CS1001: Identifier expected
                    DiagnosticResult.CompilerError("CS1001").WithSpan(10, 71, 10, 74),
                    // Test0.cs(10,71): error CS1026: ) expected
                    DiagnosticResult.CompilerError("CS1026").WithSpan(10, 71, 10, 74),
                    // Test0.cs(10,77): error CS0103: The name 'i' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(10, 77, 10, 78).WithArguments("i"),
                    // Test0.cs(10,80): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(10, 80, 10, 85),
                    // Test0.cs(10,80): error CS1513: } expected
                    DiagnosticResult.CompilerError("CS1513").WithSpan(10, 80, 10, 85),
                    // Test0.cs(10,80): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    DiagnosticResult.CompilerError("CS0214").WithSpan(10, 80, 10, 86),
                    // Test0.cs(10,86): error CS0209: The type of a local declared in a fixed statement must be a pointer type
                    DiagnosticResult.CompilerError("CS0209").WithSpan(10, 86, 10, 86),
                    // Test0.cs(10,86): error CS0210: You must provide an initializer in a fixed or using statement declaration
                    DiagnosticResult.CompilerError("CS0210").WithSpan(10, 86, 10, 86),
                    // Test0.cs(10,86): error CS1001: Identifier expected
                    DiagnosticResult.CompilerError("CS1001").WithSpan(10, 86, 10, 87),
                    // Test0.cs(10,86): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(10, 86, 10, 87),
                    // Test0.cs(10,86): error CS1003: Syntax error, '(' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(10, 86, 10, 87).WithArguments("("),
                    // Test0.cs(10,86): error CS1026: ) expected
                    DiagnosticResult.CompilerError("CS1026").WithSpan(10, 86, 10, 87),
                    // Test0.cs(10,86): error CS1031: Type expected
                    DiagnosticResult.CompilerError("CS1031").WithSpan(10, 86, 10, 87),
                    // Test0.cs(10,86): error CS1525: Invalid expression term '}'
                    DiagnosticResult.CompilerError("CS1525").WithSpan(10, 86, 10, 87).WithArguments("}"),
                    // Test0.cs(10,87): error CS1597: Semicolon after method or accessor block is not valid
                    DiagnosticResult.CompilerError("CS1597").WithSpan(10, 87, 10, 88),
                    // Test0.cs(12,5): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                    DiagnosticResult.CompilerError("CS0825").WithSpan(12, 5, 12, 8),
                    // Test0.cs(12,35): error CS0103: The name 'expr1' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(12, 35, 12, 40).WithArguments("expr1"),
                    // Test0.cs(13,30): error CS0742: A query body must end with a select clause or a group clause
                    DiagnosticResult.CompilerError("CS0742").WithSpan(13, 30, 13, 35),
                    // Test0.cs(13,30): error CS0743: Expected contextual keyword 'on'
                    DiagnosticResult.CompilerError("CS0743").WithSpan(13, 30, 13, 35),
                    // Test0.cs(13,30): error CS0744: Expected contextual keyword 'equals'
                    DiagnosticResult.CompilerError("CS0744").WithSpan(13, 30, 13, 35),
                    // Test0.cs(13,30): error CS1001: Identifier expected
                    DiagnosticResult.CompilerError("CS1001").WithSpan(13, 30, 13, 35),
                    // Test0.cs(13,30): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(13, 30, 13, 35),
                    // Test0.cs(13,30): error CS1003: Syntax error, 'in' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 30, 13, 35).WithArguments("in"),
                    // Test0.cs(13,30): error CS1525: Invalid expression term 'fixed'
                    DiagnosticResult.CompilerError("CS1525").WithSpan(13, 30, 13, 35).WithArguments("fixed"),
                    // Test0.cs(13,30): error CS1525: Invalid expression term 'fixed'
                    DiagnosticResult.CompilerError("CS1525").WithSpan(13, 30, 13, 35).WithArguments("fixed"),
                    // Test0.cs(13,30): error CS1525: Invalid expression term 'fixed'
                    DiagnosticResult.CompilerError("CS1525").WithSpan(13, 30, 13, 35).WithArguments("fixed"),
                    // Test0.cs(13,36): error CS1642: Fixed size buffer fields may only be members of structs
                    DiagnosticResult.CompilerError("CS1642").WithSpan(13, 36, 13, 36),
                    // Test0.cs(13,36): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                    DiagnosticResult.CompilerError("CS1663").WithSpan(13, 36, 13, 36),
                    // Test0.cs(13,36): error CS1001: Identifier expected
                    DiagnosticResult.CompilerError("CS1001").WithSpan(13, 36, 13, 38),
                    // Test0.cs(13,36): error CS1003: Syntax error, ',' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 36, 13, 38).WithArguments(","),
                    // Test0.cs(13,36): error CS1003: Syntax error, '[' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 36, 13, 38).WithArguments("["),
                    // Test0.cs(13,36): error CS1031: Type expected
                    DiagnosticResult.CompilerError("CS1031").WithSpan(13, 36, 13, 38),
                    // Test0.cs(13,36): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    DiagnosticResult.CompilerError("CS0214").WithSpan(13, 36, 13, 57),
                    // Test0.cs(13,36): error CS7092: A fixed buffer may only have one dimension.
                    DiagnosticResult.CompilerError("CS7092").WithSpan(13, 36, 13, 57),
                    // Test0.cs(13,39): error CS0103: The name 'expr2' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(13, 39, 13, 44).WithArguments("expr2"),
                    // Test0.cs(13,45): error CS1003: Syntax error, ',' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 45, 13, 47).WithArguments(","),
                    // Test0.cs(13,48): error CS1003: Syntax error, ',' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 48, 13, 49).WithArguments(","),
                    // Test0.cs(13,50): error CS1003: Syntax error, ',' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 50, 13, 56).WithArguments(","),
                    // Test0.cs(13,57): error CS0443: Syntax error; value expected
                    DiagnosticResult.CompilerError("CS0443").WithSpan(13, 57, 13, 57),
                    // Test0.cs(13,57): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(13, 57, 13, 62),
                    // Test0.cs(13,57): error CS1003: Syntax error, ',' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 57, 13, 62).WithArguments(","),
                    // Test0.cs(13,57): error CS1003: Syntax error, ']' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 57, 13, 62).WithArguments("]"),
                    // Test0.cs(13,63): error CS0246: The type or namespace name 'select' could not be found (are you missing a using directive or an assembly reference?)
                    DiagnosticResult.CompilerError("CS0246").WithSpan(13, 63, 13, 69).WithArguments("select"),
                    // Test0.cs(13,63): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                    DiagnosticResult.CompilerError("CS1663").WithSpan(13, 63, 13, 69),
                    // Test0.cs(13,70): error CS0102: The type 'QueryExpressionTest' already contains a definition for ''
                    DiagnosticResult.CompilerError("CS0102").WithSpan(13, 70, 13, 70).WithArguments("QueryExpressionTest", ""),
                    // Test0.cs(13,70): error CS1642: Fixed size buffer fields may only be members of structs
                    DiagnosticResult.CompilerError("CS1642").WithSpan(13, 70, 13, 70),
                    // Test0.cs(13,70): error CS0836: Cannot use anonymous type in a constant expression
                    DiagnosticResult.CompilerError("CS0836").WithSpan(13, 70, 13, 73),
                    // Test0.cs(13,70): error CS1001: Identifier expected
                    DiagnosticResult.CompilerError("CS1001").WithSpan(13, 70, 13, 73),
                    // Test0.cs(13,70): error CS1003: Syntax error, '[' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 70, 13, 73).WithArguments("["),
                    // Test0.cs(13,70): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    DiagnosticResult.CompilerError("CS0214").WithSpan(13, 70, 13, 79),
                    // Test0.cs(13,70): error CS7092: A fixed buffer may only have one dimension.
                    DiagnosticResult.CompilerError("CS7092").WithSpan(13, 70, 13, 79),
                    // Test0.cs(13,76): error CS0103: The name 'i' does not exist in the current context
                    DiagnosticResult.CompilerError("CS0103").WithSpan(13, 76, 13, 77).WithArguments("i"),
                    // Test0.cs(13,79): error CS0443: Syntax error; value expected
                    DiagnosticResult.CompilerError("CS0443").WithSpan(13, 79, 13, 79),
                    // Test0.cs(13,79): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(13, 79, 13, 84),
                    // Test0.cs(13,79): error CS1003: Syntax error, ',' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 79, 13, 84).WithArguments(","),
                    // Test0.cs(13,79): error CS1003: Syntax error, ']' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 79, 13, 84).WithArguments("]"),
                    // Test0.cs(13,79): error CS1513: } expected
                    DiagnosticResult.CompilerError("CS1513").WithSpan(13, 79, 13, 84),
                    // Test0.cs(13,85): error CS0102: The type 'QueryExpressionTest' already contains a definition for ''
                    DiagnosticResult.CompilerError("CS0102").WithSpan(13, 85, 13, 85).WithArguments("QueryExpressionTest", ""),
                    // Test0.cs(13,85): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                    DiagnosticResult.CompilerError("CS0214").WithSpan(13, 85, 13, 85),
                    // Test0.cs(13,85): error CS1642: Fixed size buffer fields may only be members of structs
                    DiagnosticResult.CompilerError("CS1642").WithSpan(13, 85, 13, 85),
                    // Test0.cs(13,85): error CS1663: Fixed size buffer type must be one of the following: bool, byte, short, int, long, char, sbyte, ushort, uint, ulong, float or double
                    DiagnosticResult.CompilerError("CS1663").WithSpan(13, 85, 13, 85),
                    // Test0.cs(13,85): error CS0443: Syntax error; value expected
                    DiagnosticResult.CompilerError("CS0443").WithSpan(13, 85, 13, 86),
                    // Test0.cs(13,85): error CS1001: Identifier expected
                    DiagnosticResult.CompilerError("CS1001").WithSpan(13, 85, 13, 86),
                    // Test0.cs(13,85): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(13, 85, 13, 86),
                    // Test0.cs(13,85): error CS1003: Syntax error, '[' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 85, 13, 86).WithArguments("["),
                    // Test0.cs(13,85): error CS1003: Syntax error, ']' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(13, 85, 13, 86).WithArguments("]"),
                    // Test0.cs(13,85): error CS1031: Type expected
                    DiagnosticResult.CompilerError("CS1031").WithSpan(13, 85, 13, 86),
                    // Test0.cs(14,3): error CS1022: Type or namespace definition, or end-of-file expected
                    DiagnosticResult.CompilerError("CS1022").WithSpan(14, 3, 14, 4),
                    // Test0.cs(15,1): error CS1022: Type or namespace definition, or end-of-file expected
                    DiagnosticResult.CompilerError("CS1022").WithSpan(15, 1, 15, 2),
                },
                FixedCode = code,
            }.RunAsync();
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530980")]
        public async Task TestReferenceInCref()
        {
            // Parsing doc comments as simple trivia; we don't know System is unnecessary, but CS8019 is disabled so
            // no diagnostics are reported.
            var code = """
                using System;
                /// <summary><see cref="String" /></summary>
                class C
                {
                }
                """;
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    DocumentationMode = DocumentationMode.None,
                },
            }.RunAsync();

            // fully parsing doc comments; System is necessary
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    DocumentationMode = DocumentationMode.Parse,
                },
            }.RunAsync();

            // fully parsing and diagnosing doc comments; System is necessary
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    DocumentationMode = DocumentationMode.Diagnose,
                },
            }.RunAsync();
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/751283")]
        public async Task TestUnusedUsingOverLinq()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                [|using System;
                {|IDE0005:using System.Linq;
                using System.Threading.Tasks;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        Console.WriteLine();
                    }
                }
                """,
                """
                using System;

                class Program
                {
                    static void Main(string[] args)
                    {
                        Console.WriteLine();
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1323")]
        public async Task TestUsingsInPPRegionWithoutOtherMembers()
        {
            await VerifyCS.VerifyCodeFixAsync(
                """
                #if true
                [|{|IDE0005:using System;|}|]
                #endif
                """,
                """
                #if true
                #endif
                """);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/20377")]
        public async Task TestWarningLevel(int warningLevel)
        {
            var code = """
                [|{|IDE0005:using System;
                using System.Collections.Generic;
                using System.Linq;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
                """;
            var fixedCode = warningLevel switch
            {
                0 => code,
                _ => """
                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
                """,
            };

            var markupMode = warningLevel switch
            {
                // Hidden diagnostics are not reported for warning level 0
                0 => MarkupMode.Ignore,

                // But are reported for all other warning levels
                _ => MarkupMode.Allow,
            };

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    MarkupHandling = markupMode,
                },
                FixedCode = fixedCode,
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var compilationOptions = (CSharpCompilationOptions)solution.GetRequiredProject(projectId).CompilationOptions!;
                        return solution.WithProjectCompilationOptions(projectId, compilationOptions.WithWarningLevel(warningLevel));
                    },
                },
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58972")]
        public async Task TestWhitespaceBeforeUnusedUsings_FileScopedNamespace()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                namespace N;

                [|{|IDE0005:using System;|}
                using System.Collections.Generic;|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        var argList = new List<string>(args);
                    }
                }
                """,
                FixedCode =
                """
                namespace N;

                using System.Collections.Generic;

                class Program
                {
                    static void Main(string[] args)
                    {
                        var argList = new List<string>(args);
                    }
                }
                """,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45866")]
        public async Task TestUsingGroups_DeleteLeadingBlankLinesIfFirstGroupWasDeleted_SingleUsing()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                [|{|IDE0005:using System;|}

                using System.Collections.Generic;|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        var argList = new List<string>(args);
                    }
                }
                """,
                FixedCode =
                """
                using System.Collections.Generic;

                class Program
                {
                    static void Main(string[] args)
                    {
                        var argList = new List<string>(args);
                    }
                }
                """
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45866")]
        public async Task TestUsingGroups_DeleteLeadingBlankLinesIfFirstGroupWasDeleted_MultipleUsings()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                [|{|IDE0005:using System;
                using System.Threading.Tasks;|}

                using System.Collections.Generic;|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        var argList = new List<string>(args);
                    }
                }
                """,
                FixedCode =
                """
                using System.Collections.Generic;

                class Program
                {
                    static void Main(string[] args)
                    {
                        var argList = new List<string>(args);
                    }
                }
                """
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45866")]
        public async Task TestUsingGroups_NotAllFirstGroupIsDeleted()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                [|{|IDE0005:using System;|}
                using System.Threading.Tasks;

                using System.Collections.Generic;|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        var argList = new List<string>(args);
                        Task task = null;
                    }
                }
                """,
                FixedCode =
                """
                using System.Threading.Tasks;

                using System.Collections.Generic;

                class Program
                {
                    static void Main(string[] args)
                    {
                        var argList = new List<string>(args);
                        Task task = null;
                    }
                }
                """
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45866")]
        public async Task TestUsingGroups_AllLastGroupIsDeleted()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                [|using System.Collections.Generic;

                {|IDE0005:using System;
                using System.Threading.Tasks;|}|]

                class Program
                {
                    static void Main(string[] args)
                    {
                        var argList = new List<string>(args);
                    }
                }
                """,
                FixedCode =
                """
                using System.Collections.Generic;

                class Program
                {
                    static void Main(string[] args)
                    {
                        var argList = new List<string>(args);
                    }
                }
                """
            }.RunAsync();
        }
    }
}