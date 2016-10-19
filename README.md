Monglot
===============

A program that creates glitch images by applying [non]random and [un]controllable transformations to [un]compressed data. The resultant image is an amalgamation of the original visual image æffected by alterations in its own binary composition. This is how many of the common glitch aesthetics (fragmentation, heterodynes, quantization error, etc.) come to fruition.

Monglot was originally conceptualized as an educational tool by [ЯOSΛ MEИKMΛN](http://rosa-menkman.blogspot.com) (and programmed by Johan Larsby), and the Win32 port of Monglot was created with that goal in mind.

### Differences from the original
* Saves all files (if **Conserve** is checked) to `%USERPROFILE%\Documents\Monglot\`
* Win processes them in little-endian format (resulting in different æffects)
* Removed **GFD**, since it does the same thing as **Folder Batch**
* Wordpad glitch now 100% accurate (added case to ignore 0D followed by 0A)
* Added **Random** choice to transcoder list; randomizes transcoder with every glitch pass
* Replaced **FIT** with **SAVE**, which saves the current on-screen glitch
* All conversion and processing is now done in memory
* Improved the Progress/Order modulation
* Takes a while to process JPG 2000 due to the non-native discrete cosine transforms

Other than the differences above, the Win32 port of Monglot works the same as noted in in the [original Monglot documentation](http://rosa-menkman.blogspot.com/2011/01/monglot.html).

### Building Monglot
Simply build it from the source in Visual Studio. There may be additional steps required based on your version of VS; this program was compiled with Visual Studio 2015.

### Notes
This repository will soon contain the original Monglot as well as this Win32 port once I get around to implementing the same fixes (maybe some/all of the same changes) as I have during the making of the Win32 version.