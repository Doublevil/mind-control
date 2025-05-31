# Introduction

This guide is intended to demonstrate how to use the `MindControl` .net library to interact with the memory of a running process. However, to make sure your understanding of the basics of memory hacking is aligned with the library's design, we will first cover some fundamental concepts. If you are new to memory hacking, this guide will help you get started on your journey. If you are already familiar with process hacking, you can skip ahead to the [Project setup](./project-setup/creating-mc-project.md) section.

## What is memory hacking and what can we do with it?

Memory hacking is basically manipulating the internal values used by a process that is running on your system, while it's running.

Every program that runs on your system is loaded into memory. This memory stores everything the program needs to run, including the values of variables, the code that is being executed, and the data that is being processed. By changing these values, you can manipulate the behavior of the program.

Usually, each program minds its own business and has a separate set of memory. However, using system functions, you can read and write to the memory of any process running on your system. A memory hacking program will make use of these functions to access the memory of its target program.

This technique is often used in gaming, to build all sorts of tools, such as game trainers, cheats, bots, overlays, and automation tools. Mods can also be built using memory hacking, although they typically also require other skills. It can also be used in general-purpose software, for debugging, reverse engineering, security testing, and more.

> [!WARNING]
> Just a reminder before you keep going: **using memory hacking for cheating in online games or to gain any kind of advantage (no matter how small) in a competitive space is wrong and will get you banned**. Always respect the rules of the games you play. Keep it fair and don't ruin the fun for others. **If you ignore this, I will do my best to shut down your project.**

## What MindControl is about

MindControl is a .net library that provides a set of tools to interact with the memory of a running process. It allows you to easily build memory hacking programs that read and write values, search for patterns, inject code, and more. It's designed to be simple to use, reliable, and efficient.

As stated before, operating systems like Windows provide functions to do that already. However, these functions are low-level, complex to understand and cumbersome to use. MindControl provides an additional layer on top of that, that considerably simplifies the process.

## Next step

The next section will be about gathering everything you need to follow this guide.