<h1 align="center">Fahrenheit</h1>
<h3 align="center">A Final Fantasy X reverse-engineering project and mod framework</h3>

<p align="center">
<img alt="Logo Banner" src="https://github.com/peppy-enterprises/fahrenheit/blob/main/assets/fh_banner.png"/>
<br/>
<h4 align="center">See the world as you never have before.</h3>
<p align="center">Art by <a href="https://mnemorie.etsy.com">Mnemorie</a></p>

## What is Fahrenheit?
Fahrenheit is a reverse-engineering project for the [Final Fantasy X and X-2 HD Remasters](https://store.steampowered.com/app/359870/).

It is also, in source-code form, a mod framework for the games. It allows you to freely hook game functions and distribute mods
in the form of loadable DLLs. Fahrenheit bootstraps and hosts the [.NET runtime](https://dotnet.microsoft.com/en-us/download)
within the games, allowing you to write mods in any compatible language.

The knowledge gathered by the project underpins many tools and mods for the game, such as the
[AI/VI TAS](https://github.com/coderwilson/FFX_TAS_Python), [Cutscene Remover](https://github.com/erickt420/FFXCutsceneRemover) mod,
[Karifean](https://github.com/Karifean)'s [FFXDataParser](https://github.com/Karifean/FFXDataParser), and more.

## Why?
Many people have made tremendous efforts to tackle this game over the years, and some have even
succeeded in creating amazing mods for these timeless games. Unfortunately, almost all such efforts were
made by lone individuals who chose not to publicize their methods and share their knowledge.
Even when they did, their knowledge may have been lost to time and is exceptionally hard to find today.

Fahrenheit attempts to put an end to this.

It aims not only to exceed all these efforts in scope, but also to stay free for others to analyze,
improve, learn from and use- now and forever.

## Building from source and testing
Building requires Visual Studio 2022 with the [built-in `vcpkg` manager enabled](https://devblogs.microsoft.com/cppblog/vcpkg-is-now-included-with-visual-studio/), and the following workloads:
- .NET desktop development (latest version)
- C++ desktop development (latest version)

To install/test your development build:
- Build output is found under ``artifacts\localdeploy`` in the directory in which you cloned Fahrenheit.
- Copy the contents of this directory (the folders ``bin``, ``modules``, etc.) to a subfolder named ``fahrenheit`` in your game directory (where ``FFX.exe`` is).
- Create an empty, extensionless file called `loadorder` in the `modules` directory. Add the DLL names of modules you wish to load, each on their own line.
- Open a terminal in ``fahrenheit/bin``, then ``.\fhstage0.exe ..\..\FFX.exe``.
- Debugging can be performed from Visual Studio. Attach to either ``fhstage0.exe`` or ``FFX.exe``, and make sure to enable [mixed-mode debugging](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-debug-managed-and-native-code?view=vs-2022).

## What's next?
Time permitting, the goals (in no specific order) of the project are:
- Provide actual code-behind, helper functions, and tooling to make various modding tasks approachable.
- Provide a mod manager for end users who simply want to enjoy the game.
- Provide quality documentation for various implementation-specific details and game systems.
- In general, _polish_ every aspect of the solution.

## Can I contribute?
Yes. Feel free to join us in [Cid's Salvage Ship](https://discord.gg/AGx2grw9nD), a Discord server that supports Fahrenheit and related efforts.

## License
Fahrenheit source code is licensed under the [MIT](https://github.com/fkelava/fahrenheit/blob/main/LICENSE.txt) license.

Assets (the contents of the ``assets`` folder) may be used in forks of Fahrenheit, but _not_ for any other purpose!

For third-party code license notices, please see
[THIRD-PARTY-NOTICES](https://github.com/fkelava/fahrenheit/blob/main/THIRD-PARTY-NOTICES.txt).

## Associated and/or derived projects
You should check out the following projects if you want finished mods or approachable tools.
- The [FFXDataParser](https://github.com/Karifean/FFXDataParser) by [Karifean](https://github.com/Karifean)
allows practical inspection and manipulation of an incredibly wide range of game data.
- The [Cutscene Remover mod for FFX](https://github.com/erickt420/FFXCutsceneRemover) by B3dIntruder, Cereth, CrimsonInferno, Flobber, Roosta, shenef, and peppy
strongly enhanced our understanding of the game and its scripting system, and remains a popular and powerful mod.
- The [FFXProjectEditor](https://github.com/osdanova/FFXProjectEditor) by [osdanova](https://github.com/osdanova)
allows both file and memory manipulation with an easy-to-use interface.
- The [FFX Rando](https://github.com/nyterage/FFXRando) by [Taeznak](https://github.com/nyterage)
is a highly versatile, Fahrenheit-compatible randomizer.
