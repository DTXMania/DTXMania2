bjxa - BandJAM XA audio codec
=============================

This is an audio codec for BandJAM XA audio files used in several games, some
of them open source. Unfortunately the source code for xa.exe and xadec.dll
was not available and limited to Windows 32 bit systems. The libbjxa library
and bjxa program offer a portable interface to decode XA files.

Because CDROM XA from which BandJAM XA derives is a lossy codec, it is better
not to convert existing XA files into WAVE files to then re-encode them with a
more modern lossy codec.

This is the result of a reverse engineering project to not only offer a
decoder for the file format but also document it.

Installation
------------

Grab the `latest release`_ archive and once inside the source directory,
follow these steps::

    $ ./configure
    $ make
    $ make check # optional but recommended
    $ sudo make install

If the project fails to build, it might be because your system or linker does
not support version scripts. In this case bjxa can be configured without the
version script support::

    $ ./configure --without-ld-version-script

To learn more about available configuration options, you can run and inspect
the output of ``./configure --help``.

.. _`latest release`: https://github.com/dridi/bjxa/releases/latest

RPM Packaging
-------------

Instead of directly installing the package you can build an RPM::

    make rpm

The resulting packages can be found in the ``rpmbuild`` directory in your
build tree.

If you need to build an RPM for a different platform you may use ``mock(1)``
with the proper ``--root`` option. All you got to do is run ``make mockbuild``
and set the desired flags in the ``MOCK_OPTS`` variable. For instance, to
build RPMs for CentOS 7::

    make mockbuild MOCK_OPTS='--root epel-7-x86_64'

The resulting packages can be found in the ``mockbuild`` directory in your
build tree.

Documentation
-------------

Once installed, you should have access to comprehensive manuals describing how
to use the libbjxa library or bjxa program, and the BandJAM XA file format.

In particular, the ``bjxa(3)`` manual comes with a code example showing how
the actual ``bjxa(1)`` program uses the library to decode XA files. The
``bjxa(5)`` manual describes the BandJAM XA file format, loosely based on the
CDROM XA standard for real time compressed audio on some CDROM-based systems.

Portability
-----------

bjxa has been successfully tested on the following systems:

- FreeBSD
- GNU/Linux (Fedora)
- Illumos (OmniOS)
- NetBSD

It has been tested on Fedora for the following architectures:

- aarch64
- armv7hl
- i686
- ppc64
- ppc64le
- s390x
- x86_64 (amd64)

bjxa has been partially cross-compiled for Windows, testing using Wine fails
halfway through. To build it on a Unix-like system with MinGW, you can try
this::

    $ ./configure \
    >        --host=<ARCH>-w64-mingw32 \
    >        --disable-static \
    >        CFLAGS=" \
    >            -Wno-pedantic \
    >            -std=gnu99 \
    >            -U_POSIX_C_SOURCE \
    >            -U_XOPEN_SOURCE \
    >        "
    $ make LDFLAGS=-no-undefined

And replace ``<ARCH>`` with the desired architecture.

Windows Support
---------------

The project should in theory also work on Windows but it hasn't been verified.
However there is a .NET variant of bjxa written in C#. Starting with version
0.2 a Zip file containing the source code and Windows executables is available
on release pages. The DLL and EXE are however not built and tested on Windows,
instead Mono is used for building and Wine for testing.

Assuming the installation instructions were followed to build from source, and
assuming the Mono development files are available on the system, the following
steps will reconfigure the project and build the C# code::

    $ ./configure --with-dotnet --enable-silent-rules
    [...]
    $ make
      CCLD     dotnet/libbjxa.dll
      CCLD     dotnet/bjxa.exe
      GEN      dotnet/bjxa-0.2.zip

The test suite currently doesn't test the C# code, it has been manually tested
to produce bit-for-bit identical WAVE files.

It is possible to specify a C# compiler and command line options to the
compiler, for example to enable debug assertions::

    $ ./configure --with-dotnet CSC=past/to/csc CSFLAGS=-d:DEBUG

Hacking
-------

bjxa relies on autotools for building, and a range of tools for testing
and code coverage. The basic usage is as follows::

   $ path/to/bjxa/bootstrap \
   >        [--enable-asan] \
   >        [--enable-msan] \
   >        [--enable-ubsan] \
   >        [--enable-lcov] \
   >        [--without-ld-version-script]
   $ make check

The first command will reveal the missing bits, and the second the potential
failures. Code coverage MUST be turned off when the test suite is used for
checking because it turns off assertions.

The ``bootstrap`` script needs to be run only once. In order to reconfigure
the build tree, you can use autoconf's ``configure`` script. Command-line
arguments to the ``bootstrap`` script are passed to ``configure``.

By default the ``bootstrap`` script configures the build tree to include .NET
support. To only build the C library from git, pass the ``--without-dotnet``
argument to the ``bootstrap`` execution.

For code coverage, the simplest way to get a report is as follows::

   $ path/to/bjxa/bootsrap --enable-lcov
   $ make lcov
   $ xdg-open lcov/index.html

One goal is to maintain the 100% coverage of the C library.
