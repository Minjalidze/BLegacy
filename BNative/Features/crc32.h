#ifndef _EXT_CRC32_H_
#define _EXT_CRC32_H_

#include <windows.h>

class CRC32
{
private:	
	void Init();

public:
	unsigned int result;

	CRC32()
	{
		Init();
	}
	
	CRC32(const void* data, size_t length)
	{
		Init();
		Hash(data, length);
	}

	void Hash(const void* data, size_t length);
};

#endif