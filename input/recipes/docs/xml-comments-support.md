Order: 5
Description: Supported XML comments
---

Up until v2.2.9, Wyam supported the [standard XML comments](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags), except ```<see langword />```.

## Wyam2 v3.0.0
Added support for:
- **revisionHistory**, as defined by [SHFB](http://ewsoftware.github.io/XMLCommentsGuide/html/2a973959-9c9a-4b3b-abcb-48bb30382400.htm)
    - The ```IDocument``` returned by the CodeAnalysis module has an additional ```List<RevisionComment>``` containing the list of revisions defined in the revision history block
    - the visibility attributes on both _revisionHistory_ and _revision_ elements are taken into consideration when parsing
- **code** attributes _language_, _source_ and _region_, as defined by [SHFB](http://ewsoftware.github.io/XMLCommentsGuide/html/1abd1992-e3d0-45b4-b43d-91fcfc5e5574.htm)
    - it does not support nested code blocks since the same thing can be achieved by using 2 _code_ blocks and any kind of text in-between
- **definition list**, as defined by [SHFB](http://ewsoftware.github.io/XMLCommentsGuide/html/e433d846-db15-4ac8-a5f5-f3428609ae6c.htm)
- **note**, as defined by [SHFB](http://ewsoftware.github.io/XMLCommentsGuide/html/4302a60f-e4f4-4b8d-a451-5f453c4ebd46.htm)
    - it is rendered as a ```<div>``` with css class _note type_, for example ```<div class="tip">Always update documentation!</div>```
- **b**(bold) and **i**(italics) since they're supported by Visual Studio and Rider intellisense
    - Wyam will render any unknown tags as they are so it is possible to add HTML tags inside XML comments but they cannot contain any XML comment element because it will not be parsed and converted into HTML
    - content of ```<b></b>``` and ```<i></i>``` elements is parsed so they can contain special XML markup like ```<see cref="SomeClass" />```
- **see langword**
    - ```<see langword="null" />``` will be rendered as ```<code>null</code>```

## Other SHFB specific elements

- **event** is not supported but it is possible to select class methods and retrieve their type and summary
- **preliminary** can be retrieved from ```OtherComment```, see [this](https://github.com/Wyam2/wyam/blob/1ae999a995113ee9cc719d709699d6df76a5bb74/tests/extensions/Wyam.CodeAnalysis.Tests/AnalyzeCSharpXmlDocumentationFixture.cs#L166) test
- **threadsafety** can also be retrieved from ```OtherComment```
- **nested code elements** since the same can be acomplish by defining independent ```code``` blocks
- **AttachedEventComments** is not supported
- **AttachedPropertyComments** is not supported
- **Code Contract elements** is not supported
- **conceptualLink** is not supported
- **exclude** is not supported
- **filterpriority** is not supported
- **overloads** is not supported
- **token** is not supported

