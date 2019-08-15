# BHLHandwritingAnalyzer
## Overview
This is a proof of concept intended to show how Microsoft Azure Cognitive Services, specifically the Computer Vision service, can be used to produce OCR text of handwriting.  It also illustrates how this OCR output is good enough for use in identifying scientific names mentioned in the text.  For extracting the scientific names from the text, the gnfinder command-line tool from the Global Names project is used.

More information about the Computer Vision service is available at https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/.  As of August 2019, the free tier of this Azure service allows it to be called 5000 times per month.

More information about the gnfinder tool is available at https://github.com/gnames/gnfinder.

The BHLHandwritingAnalyzer tool is written in C# and utilizes .NET Core 2.1 (free, open-source, and cross-platform).  It can be compiled to run on Windows, OSX, or Linux.  The gnfinder CLI can also be run on Windows, OSX, or Linux.

## Using the tool

The compiled tool is executed from the command line as follows:

    BHLHandwritingAnalyzer BHL-ITEM-ID

where BHL-ITEM-ID is the identifier of a BHL Item (a book in BHL).  For best results, choose handwritten manuscripts (rather than printed material) to be analyzed by this tool.

When executed, the tool performs the following actions:

1. Invokes the BHL API to download the text for each page of the book.
2. Invokes the BHL API to download the scientific names associated with each page of the book.
3. Submits the page images to the Azure Computer Vision service.
4. Parses the Azure service response, saving the OCR text produced for each page.
5. Submits the OCR text for each page to the gnfinder tool, and saves the gnfinder responses.
6. Compiles the scientific names downloaded from BHL into a single document with all names associated with the book in BHL.
7. Compiles the scientific names returned by gnfinder into a single document with all names found in the OCR produced by the Azure service. 
   
More information about the BHL API is available at https://www.biodiversitylibrary.org/docs/api3.html.

## Setting Up The Tool
To set up your environment to run this tool, do the following:

1. Install .NET Core 2.1 or later.
2. Download the code from this repository.
3. Get a BHL API key at https://www.biodiversitylibrary.org/getapikey.aspx.  
4. Create an Azure Cognitive Services subscription and get the associated Azure key and endpoint.  See https://docs.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account for more information.
5. Update the appsettings.json file with the BHL API key, Azure subscription key, and Azure service endpoint.
6. On the command line, navigate to the folder that contains the project file (BHLHandwritingAnalyzer.csproj).
7. Compile the tool for your environment with one of the following commands:
   1. dotnet publish -r win-x86 BHLHandwritingAnalyzer.csproj
   2. dotnet publish -r win-x64 BHLHandwritingAnalyzer.csproj
   3. dotnet publish -r osx-x64 BHLHandwritingAnalyzer.csproj
   4. dotnet publish -r linux-x64 BHLHandwritingAnalyzer.csproj

## Analysis of the Tool Output

The "Example" folder contains the output files produced when running the tool for BHL Item 239195.  Included is: 
1. The original text ("txt" files) and scientific names ("xml" files) for each page, downloaded from BHL.  These are contained in the "output/original" folder.
2. A "csv" file that compiles all of the original scientific names for the item.  This is contained in the "output/original" folder. 
3. The OCR output of the Azure Computer Vision service ("txt" files) and scientific names output of the gnfinder tool ("json" files) for each page.  These are contained in the "output/new" folder.
4. A "csv" file that compiles all of the scientific names identified by gnfinder for the item.  This is contained in the "output/new" folder. 

Here are some examples of the output of the tool.

For page 54756329 (https://www.biodiversitylibrary.org/page/54756329), the original BHL text is contained in the "output/original/54756329.txt" file.  The file is empty (no text).  The text output by the Azure Computer Vision service is contained in the "output/new/54756329.txt" file, which is shown here:

    8946 - may 2, 1961
    with sagebrush and Beatlered
    Junipers in sandsone country
    about kane barings, 15 mills
    south of most, can Juan Co,
    Elevation 5100 feet. strongly
    armed shrubs about In. tall
    Flamers bright yellow,
    very fragrant, attracting bees.
    Berberis fremontic Torr.
    Berberin 4

Compared to a human transcription of the text, the Azure output is not perfect.  It is impressive though, considering that it is analyzing handwriting, and not printed text.  Here is the human transcription of the text:

    8940 - may 2, 1961
    With sagebrush and scattered
    junipers in sandsone country
    about Kane Springs, 15 miles
    south of Moab, San Juan Co.,
    Utah.  Twp 28S., R. 22 E., S1.
    Elevation 5100 feet. Strongly
    armed shrubs about 2 m. tall
    Flowers bright yellow,
    very fragrant, attracting bees.
    Berberis fremontii Torr.
    Berberis 4

The list of original scientific names is contained in "output/original/54756329_names.xml".  This file is also empty.  The output of the gnfinder tool for the output of the Azure Computer Vision service is contained in "output/original/54756329_names.json", a portion of which is shown here:

    {
    "metadata": {
        "total_words": 45,
        "total_candidates": 10,
        "total_names": 1
    },
    "names": [
        {
        "type": "Uninomial",
        "verbatim": "Berberis",
        "name": "Berberis",
        "odds": 76158.25464939173,
        "odds_details": {...},
        "start": 256,
        "end": 264,
        "annotation": "",
        "verification": {...}
        }
    ]
    }

Finally, all of the scientific names associated with the item in BHL are contained in the file "output/original/AllOriginalNames239195.tsv".  There are no names in the list.  All of the scientific names found by gnfinder in the text output of the Azure Computer Vision service are found in the file "output/new/AllNewNames239195.tsv".  There are 34 names, shown here:

    PageID	    Name
    54756329	Berberis
    54756327	Streptanthella longirostris
    54756325	Eleva tim
    54756325	Festuca octoflora
    54756324	Gilia
    54756323	Gilia sinuata
    54756321	Streptanthus cordatus nutt
    54756320	Cryptantha
    54756315	Physaria australis
    54756313	Cryptantha flava
    54756310	Penstemon pachyphyllus
    54756309	Penstemon eatoni
    54756308	Coanothera pallida
    54756302	Lesquerella rectipes
    54756300	Hymenoxys leptoclada
    54756299	Enceliopsis mutans
    54756298	Machaeranthera venusta
    54756297	Castilleja chromosa
    54756290	Cryptantha tennis
    54756290	Cryptanthe
    54756287	Cryptantha crassisepala
    54756286	Dithyrea
    54756285	Eremocrinum
    54756284	Saidlandia pinnatifida
    54756282	Phacelia crenulata
    54756279	Gilia
    54756277	Salix nigra
    54756276	Salix amygdaloides
    54756273	Physaria australis
    54756272	Penstemon utahensis
    54756268	Phloy longifolia nutt
    54756268	Phlox langifolia
    54756267	Cryptantha flavoculata
    54756253	Cyperaceae

From these examples it should be evident that while the OCR analysis of the handwriting does not produce flawless results, it is good enough to allow for further examination and entity extraction from the text.
