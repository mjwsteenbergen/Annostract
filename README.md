# Annostract
Do you have folders with highlights that you will never use? Do you want to do a thorough literature review? Use Annostract, the highlight extractor!

Features include:
- Extraction of highlight
- Define structure with [comments](https://github.com/mjwsteenbergen/Annostract/blob/master/Annostract/Extractor/File/FileExtractor.cs#L187-L192)
- Define indents with "<", "-", ">" and "<>"
- Extract images
- (Semi)-automatically find link to papers to read
- (Semi)-automatically generate bibliography of read papers
- Serialize to markdown, markender or latex format
- Download highlights from [Instapaper](https://www.instapaper.com/)


## Install
1. Install [.NET Core](https://dotnet.microsoft.com/download/dotnet-core)
2. To try out the tool run `dotnet tool install -g Annostract.Tool`

## Use as a library
Run `dotnet add package Annostract --version 1.1.0`
