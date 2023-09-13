#ifndef _EXT_FILE_IO_H_
#define _EXT_FILE_IO_H_

#define _CRT_SECURE_NO_WARNINGS

#include <windows.h>
#include <string.h>
#include <stdio.h>
#include <io.h>

// Exists //
bool file_exists(const char* filename);
bool file_exists(const wchar_t* filename);

// Read //
unsigned long file_readallbytes(const wchar_t *filename, char *&buffer);
unsigned long file_readallbytes(const char* filename, char *&buffer);

// Write //
unsigned long file_writeallbytes(const wchar_t *filename, char *buffer, size_t length);
unsigned long file_writeallbytes(const char* filename, char *buffer, size_t length);

#endif
