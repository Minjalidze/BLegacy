#include "file.h"
#include "utfconvert.h"

// Exists //
bool file_exists(const char* filename)
{
	FILE *file = fopen(filename, "r");
	if (file)
	{
		fclose(file);
		return true;
	}
	return false;
}

bool file_exists(const wchar_t* filename)
{
	FILE *file = _wfopen(filename, L"r");
	if (file)
	{
		fclose(file);
		return true;
	}
	return false;
}

	// Read //
#pragma region file_readallbytes(const wchar_t *filename, char *&buffer)
unsigned long file_readallbytes(const wchar_t *filename, char *&buffer)
{
	unsigned long length = 0;

	if (filename)
	{
		HANDLE handle = CreateFile(filename, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);

		if (handle != INVALID_HANDLE_VALUE)
		{
			unsigned long fileSize = GetFileSize(handle, NULL);

			if (fileSize != INVALID_FILE_SIZE)
			{
				buffer = new char[fileSize];
				ReadFile(handle, buffer, fileSize, &length, NULL);
			}

			CloseHandle(handle);
		}
	}
	return length;
}
#pragma endregion

#pragma region file_readallbytes(const char* filename, char *&buffer)
unsigned long file_readallbytes(const char* filename, char *&buffer)
{		
	return file_readallbytes((wchar_t*)utf8_to_utf16(filename).c_str(), buffer);
}
#pragma endregion


// Write //
#pragma region file_writeallbytes(const wchar_t *filename, char *buffer, size_t length)
unsigned long file_writeallbytes(const wchar_t *filename, char *buffer, size_t length)
{
	unsigned long written = 0;
	if (filename && buffer && length > 0)
	{
		HANDLE handle = CreateFile(filename, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, 0, NULL);

		if (handle != INVALID_HANDLE_VALUE)
		{
			WriteFile(handle, buffer, (unsigned long)length, &written, NULL);
			CloseHandle(handle);
		}
	}
	return written;
}
#pragma endregion

#pragma region file_writeallbytes(const char* filename, char *buffer, size_t length)
unsigned long file_writeallbytes(const char* filename, char *buffer, size_t length)
{		
	return file_writeallbytes((wchar_t*)utf8_to_utf16(filename).c_str(), buffer, length);
}
#pragma endregion
