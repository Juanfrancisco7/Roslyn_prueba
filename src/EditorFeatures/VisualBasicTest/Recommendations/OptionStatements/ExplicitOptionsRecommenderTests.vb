﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OptionStatements
    Public Class ExplicitOptionsRecommenderTests
        Inherits RecommenderTests

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OptionsAfterOptionExplicitTest()
            VerifyRecommendationsAreExactly(<File>Option Explicit |</File>, "On", "Off")
        End Sub
    End Class
End Namespace
