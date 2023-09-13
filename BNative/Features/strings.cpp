#include "strings.h"

/*
// Array :: Length
*/

#pragma region array_length(void *_array)
int array_length(void *_array)
{
	int length = 0;
	if (_array != NULL)
	{		
		void **values = (void**)_array;
		for (length = 0; values[length] != NULL; length++);
	}
	return length;
}
#pragma endregion

#pragma region array_new(void *values, ...)
void** array_new(void *values, ...)
{
	va_list args;
	void *value = nullptr;
	void **result = new void*[0];

	for (va_start(args, values); values != value; values = va_arg(args, void*))
	{
		array_append(&result, values);
		value = values;
	}
	va_end(args);
	return result;
}
#pragma endregion

#pragma region array_append(void ***_array, void *_value)
void array_append(void ***_array, void *_value)
{
	int length = 2; // min length + 1 for null

	if (*_array == NULL)
	{
		*_array = (void **)malloc(length * sizeof(*_array));
	}
	else
	{
		while ((*_array)[length-2]) length++;
		*_array = (void **)realloc(*_array, length * sizeof(*_array));
	}
	(*_array)[length-2] = _value;
	(*_array)[length-1] = NULL;
}
#pragma endregion

/*
// String :: ToLower
*/

char* to_lower(char *value)
{
	if (value)
	{		
		value = _strdup(value);
		char* ch = value;

		while (*ch) 
		{ 
			*ch = tolower(*ch); 
			ch++; 
		}
	}
	return value;
}

char* to_upper(char *value)
{
	if (value)
	{
		value = _strdup(value);
		char* ch = value;

		while (*ch) 
		{ 
			*ch = toupper(*ch); 
			ch++; 
		}
	}
	return value;
}

wchar_t* to_lower(wchar_t *value)
{
	if (value)
	{
		value = _wcsdup(value);
		wchar_t* ch = value;

		while (*ch) 
		{ 
			*ch = towlower(*ch); 
			ch++; 
		}
	}
	return value;
}

wchar_t* to_upper(wchar_t *value)
{	
	if (value)
	{
		value = _wcsdup(value);
		wchar_t* ch = value;

		while (*ch) 
		{ 
			*ch = towupper(*ch); 
			ch++; 
		}
	}
	return value;
}

/*
// String :: ToString
*/

#pragma region to_cstring(string input)
char* to_cstring(std::string input)
{
	char* result = new char[input.size() + 1]();
	strcpy(result, input.c_str());
	return result;
}
#pragma endregion

#pragma region string to_string(void *value)
std::string to_string(void *value)
{
	char buffer[_MAX_INT_DIG + 2];
	sprintf_s(buffer, sizeof(buffer), "%p", value);
	return std::string(buffer);
}
#pragma endregion

#pragma region string to_string(char value)
std::string to_string(char value)
{
	char buffer[_MAX_INT_DIG + 2];
	sprintf(buffer, "%d", value);
	return std::string(buffer);
}
#pragma endregion

#pragma region string to_string(unsigned char value)
std::string to_string(unsigned char value)
{
	char buffer[_MAX_INT_DIG + 2];
	return std::string(_itoa(value, buffer, 10));
}
#pragma endregion

#pragma region string to_string(short value)
std::string to_string(short value)
{
	char buffer[_MAX_INT_DIG + 2];
	sprintf(buffer, "%d", value);
	return std::string(buffer);
}
#pragma endregion

#pragma region string to_string(unsigned short value)
std::string to_string(unsigned short value)
{
	char buffer[_MAX_INT_DIG + 2];
	return std::string(_itoa(value, buffer, 10));
}
#pragma endregion

#pragma region string to_string(int value)
std::string to_string(int value)
{
	char buffer[_MAX_INT_DIG + 2];
	return std::string(_itoa(value, buffer, 10));
}
#pragma endregion

#pragma region string to_string(unsigned int value)
std::string to_string(unsigned int value)
{
	char buffer[_MAX_INT_DIG + 2];
	return std::string(_ultoa(value, buffer, 10));
}
#pragma endregion

#pragma region string to_string(long value)
std::string to_string(long value)
{
	char buffer[_MAX_INT_DIG + 2];
	return std::string(_ltoa(value, buffer, 10));
}
#pragma endregion

#pragma region string to_string(unsigned long value)
std::string to_string(unsigned long value)
{
	char buffer[_MAX_INT_DIG + 2];
	return std::string(_ultoa(value, buffer, 10));
}
#pragma endregion

#pragma region string to_string(long long value)
std::string to_string(long long value)
{
	char buffer[_MAX_INT_DIG + 2];
	return std::string(_i64toa(value, buffer, 10));
}
#pragma endregion

#pragma region string to_string(unsigned long long value)
std::string to_string(unsigned long long value)
{
	char buffer[_MAX_INT_DIG + 2];
	return std::string(_ui64toa(value, buffer, 10));
}
#pragma endregion

#pragma region string to_string(float value)
std::string to_string(float value)
{
	char buffer[DBL_MAX_10_EXP * 2];
	sprintf(buffer, "%f", value);
	return std::string(buffer);
}
#pragma endregion

#pragma region string to_string(double value)
std::string to_string(double value)
{
	char buffer[DBL_MAX_10_EXP + 2];
	sprintf(buffer, "%Lf", value);
	return std::string(buffer);
}
#pragma endregion

#pragma region string to_string(long double value)
std::string to_string(long double value)
{
	char buffer[DBL_MAX_10_EXP + 2];
	sprintf(buffer, "%Lf", value);
	return std::string(buffer);
}
#pragma endregion

/*
// String : WCharString
*/

wchar_t* strtowcs(const char *value)
{
	const size_t length = strlen(value) + 1;
	wchar_t* result = new wchar_t[length]();
	mbstowcs(result, value, length);
	return result;
}

char* wcstostr(const wchar_t *value)
{
	const size_t length = wcslen(value) + 1;
	char* result = new char[length]();
	wcstombs(result, value, length);
	return result;
}

/*
// String : Search
*/

size_t strpos(const char* haystack, const char* needle)
{
	const char *p = strstr(haystack, needle);
	if (p) return p - haystack;
	return -1;
}

/*
// String : Format
*/

#pragma region formatf(const char *format, ...)
char *formatf(const char *format, ...)
{
	va_list args;
	va_start(args, format);

	size_t count = vsnprintf(NULL, 0, format, args) + 1;
	char *buffer = new char[count];

	vsnprintf(buffer, count, format, args);
	va_end(args);

	return buffer;
}
#pragma endregion

#pragma region formatf(const wchar_t *format, ...)
wchar_t *formatf(const wchar_t *format, ...)
{
	va_list args;
	va_start(args, format);

	size_t count = vswprintf(NULL, 0, format, args) + 1;
	wchar_t *buffer = new wchar_t[count];

	vswprintf(buffer, count, format, args);
	va_end(args);

	return buffer;
}
#pragma endregion

/*
// String : Concat
*/

#pragma region strconcat(const char *format, ...)
char* strconcat(const char *first, ...)
{
	va_list args;
	size_t length = 0;
	char *result = NULL;

	if (first != NULL)
	{
		const char *arg = first;
		for (va_start(args, first); arg != NULL; arg = va_arg(args, const char*))
		{			
			length += strlen(arg);			
		}
		va_end(args);		

		if (length > 0)
		{
			result = new char[length + 1];
			result[length] = 0;
			strcpy(result, first);
			va_start(args, first);
			for (arg = va_arg(args, char *); arg != NULL; arg = va_arg(args, char *))
			{
				strcat(result, arg);
			}
			va_end(args);
		}
	}

	return result;
}
#pragma endregion

#pragma region wcsconcat(const wchar_t *format, ...)
wchar_t* wcsconcat(const wchar_t *first, ...)
{
	va_list args;
	size_t length = 0;
	wchar_t *result = NULL;

	if (first != NULL)
	{
		const wchar_t *arg = first;
		for (va_start(args, first); arg != NULL; arg = va_arg(args, const wchar_t*))
		{
			length += wcslen(arg);
		}
		va_end(args);

		if (length > 0)
		{
			result = new wchar_t[length + 1];
			result[length] = 0;
			wcscpy(result, first);
			va_start(args, first);
			for (arg = va_arg(args, wchar_t *); arg != NULL; arg = va_arg(args, wchar_t *))
			{
				wcscat(result, arg);
			}
			va_end(args);
		}
	}

	return result;
}
#pragma endregion

/*
// String : Trim
*/

#pragma region trim(const char *input, const char *trimChars)
char *trim(const char *input, const char *trimChars)
{
	if (input && strlen(input) > 0)
	{
		size_t length = 0;
		size_t l = strlen(input);
		size_t t = strlen(trimChars);

		while (length != l)
		{
			length = l;
			for (size_t i = 0; i < t; i++)
			{
				if (*input == trimChars[i]) input++, --l;
				if (l > 0 && input[l - 1] == trimChars[i]) l--;
			}
		}

		char *result = new char[l + 1] ();
		if (l > 0) strncpy(result, input, l);

		return result;
	}
	return (char*)input;
}
#pragma endregion

#pragma region trim(const wchar_t *input, const wchar_t *trimChars)
wchar_t *trim(const wchar_t *input, const wchar_t *trimChars)
{
	if (input && wcslen(input) > 0)
	{
		size_t length = 0;		
		size_t l = wcslen(input);
		size_t t = wcslen(trimChars);

		while (length != l)
		{
			length = l;
			for (size_t i = 0; i < t; i++)
			{				
				if (*input == trimChars[i]) input++, --l;
				if (l > 0 && input[l - 1] == trimChars[i]) l--;
			}
		}

		wchar_t *result = new wchar_t[l + 1] ();
		if (l > 0) wcsncpy(result, input, l);

		return result;
	}
	return (wchar_t*)input;
}
#pragma endregion

/*
// String : Replace
*/

#pragma region replace(const char *input, const char from, const char to)
char *replace(const char *input, const char from, const char to)
{
	if (input && strlen(input) > 0)
	{
		size_t length = strlen(input);
		char *result = new char[length+1]();

		for (size_t i = 0; i < length; i++)
		{
			if (input[i] == from)
			{
				result[i] = to;
			}
			else
			{
				result[i] = input[i];
			}
		}
		return result;
	}
	return (char*)input;
}
#pragma endregion

#pragma region replace(const wchar_t *input, const wchar_t from, const wchar_t to)
wchar_t *replace(const wchar_t *input, const wchar_t from, const wchar_t to)
{
	if (input && wcslen(input) > 0)
	{
		size_t length = wcslen(input);
		wchar_t *result = new wchar_t[length + 1] ();

		for (size_t i = 0; i < length; i++)
		{
			if (input[i] == from)
			{
				result[i] = to;
			}
			else
			{
				result[i] = input[i];
			}
		}
		return result;
	}
	return (wchar_t*)input;
}
#pragma endregion

/*
// String : Split (from left to right)
*/

#pragma region split(const char *input, const char separator, int max_chunks)
char **split(const char *input, const char separator, int max_chunks)
{
	if (input)
	{
		int length = (int)strlen(input);
		char **chunks = new char*[length];
		int count = 0, from = 0, strlen = 0;

		for (int i = 0; i < length && !(max_chunks > 0 && count >= max_chunks-1); i++)
		{
			if (input[i] == separator)
			{
				strlen = i - from;

				if (strlen > 0)
				{
					chunks[count] = new char[strlen + 1]();
					strncpy(chunks[count], &input[from], strlen);					
					count++;
				}

				from = i + 1;
			}
		}

		if (from < length)
		{
			strlen = length - from;
			chunks[count] = new char[strlen + 1]();
			strncpy(chunks[count], &input[from], strlen);
			count++;
		}

		// Перестроить массив результатов //
		char **result = new char*[count + 1]();
		memcpy(result, chunks, sizeof(char*) * count);

		delete[] chunks;

		return result;
	}

	return NULL;
}
#pragma endregion

#pragma region split(const wchar_t *input, const wchar_t separator, int max_chunks)
wchar_t **split(const wchar_t *input, const wchar_t separator, int max_chunks)
{
	if (input)
	{
		int length = (int)wcslen(input);
		wchar_t **chunks = new wchar_t*[length];
		int count = 0, from = 0, strlen = 0;

		for (int i = 0; i < length && !(max_chunks > 0 && count >= max_chunks-1); i++)
		{		
			if (input[i] == separator)
			{
				strlen = i - from;

				if (strlen > 0)
				{
					chunks[count] = new wchar_t[strlen + 1]();
					wcsncpy(chunks[count], &input[from], strlen);
					count++;
				}

				from = i + 1;
			}
		}

		if (from < length)
		{
			strlen = length - from;
			chunks[count] = new wchar_t[strlen + 1]();
			wcsncpy(chunks[count], &input[from], strlen);
			count++;
		}

		// Перестроить массив результатов //
		wchar_t **result = new wchar_t*[count + 1]();
		memcpy(result, chunks, sizeof(wchar_t*) * count);

		delete[] chunks;

		return result;
	}

	return NULL;
}
#pragma endregion

#pragma region split(const char *input, const char *separator, int max_chunks)
char **split(const char *input, const char *separator, int max_chunks)
{
	if (input)
	{
		int length = (int)strlen(input);
		int s_length = (int)strlen(separator);

		char **chunks = new char*[length];
		int count = 0, from = 0, strlen = 0;

		for (int i = 0; i < length && !(max_chunks > 0 && count >= max_chunks-1); i++)
		{
			if (strncmp(&input[i], separator, s_length) == 0)
			{
				strlen = i - from;

				if (strlen > 0)
				{
					chunks[count] = new char[strlen + 1]();
					strncpy(chunks[count], &input[from], strlen);
					count++;
				}

				from = i + s_length;
			}
		}

		if (from < length)
		{
			strlen = length - from;
			chunks[count] = new char[strlen + 1]();
			strncpy(chunks[count], &input[from], strlen);
			count++;
		}

		// Перестроить массив результатов //
		char **result = new char*[count + 1]();
		memcpy(result, chunks, sizeof(char*) * count);

		delete[] chunks;

		return result;
	}

	return NULL;
}
#pragma endregion

#pragma region split(const wchar_t *input, const wchar_t *separator, int max_chunks)
wchar_t **split(const wchar_t *input, const wchar_t *separator, int max_chunks)
{
	if (input)
	{
		int length = (int)wcslen(input);
		int s_length = (int)wcslen(separator);

		wchar_t **chunks = new wchar_t*[length];		
		int count = 0, from = 0, strlen = 0;

		for (int i = 0; i < length && !(max_chunks > 0 && count >= max_chunks-1); i++)
		{
			if (wcsncmp(&input[i], separator, s_length) == 0)
			{
				strlen = i - from;

				if (strlen > 0)
				{
					chunks[count] = new wchar_t[strlen + 1]();
					wcsncpy(chunks[count], &input[from], strlen);
					count++;
				}

				from = i + s_length;
			}
		}

		if (from < length)
		{
			strlen = length - from;
			chunks[count] = new wchar_t[strlen + 1]();
			wcsncpy(chunks[count], &input[from], strlen);
			count++;
		}

		// Перестроить массив результатов //
		wchar_t **result = new wchar_t*[count + 1]();
		memcpy(result, chunks, sizeof(wchar_t*) * count);

		delete[] chunks;

		return result;
	}

	return NULL;
}
#pragma endregion

/*
// String : Split (from right to left)
*/

#pragma region split_r(const char *input, const char separator, int max_chunks)
char **split_r(const char *input, const char separator, int max_chunks)
{
	if (input)
	{
		int length = (int)strlen(input);
		char **chunks = new char*[length];
		int count = 0, strlen = 0, end = length - 1;

		for (int i = end; i >= 0 && !(max_chunks > 0 && count >= max_chunks - 1); i--)
		{
			if (input[i] == separator)
			{
				strlen = end - i;

				if (strlen > 0)
				{
					chunks[count] = new char[strlen + 1]();
					strncpy(chunks[count], &input[i + 1], strlen);
					count++;
				}

				end = i - 1;
			}
		}

		if (end++ > 0)
		{
			chunks[count] = new char[end + 1]();
			strncpy(chunks[count], &input[0], end);
			count++;
		}


		// Перестроить массив результатов //
		char **result = new char*[count + 1]();
		memcpy(result, chunks, sizeof(char*) * count);

		delete[] chunks;

		return result;
	}

	return NULL;
}
#pragma endregion

#pragma region split_r(const wchar_t *input, const wchar_t separator, int max_chunks)
wchar_t **split_r(const wchar_t *input, const wchar_t separator, int max_chunks)
{
	if (input)
	{
		int length = (int)wcslen(input);
		wchar_t **chunks = new wchar_t*[length];
		int count = 0, strlen = 0, end = length - 1;

		for (int i = end; i >= 0 && !(max_chunks > 0 && count >= max_chunks - 1); i--)
		{
			if (input[i] == separator)
			{
				strlen = end - i;

				if (strlen > 0)
				{
					chunks[count] = new wchar_t[strlen + 1]();
					wcsncpy(chunks[count], &input[i + 1], strlen);
					count++;
				}

				end = i - 1;
			}
		}

		if (end++ > 0)
		{
			chunks[count] = new wchar_t[end + 1]();
			wcsncpy(chunks[count], &input[0], end);
			count++;
		}

		// Перестроить массив результатов //
		wchar_t **result = new wchar_t*[count + 1]();
		memcpy(result, chunks, sizeof(wchar_t*) * count);

		delete[] chunks;

		return result;
	}

	return NULL;
}
#pragma endregion

#pragma region split_r(const char *input, const char *separator, int max_chunks)
char **split_r(const char *input, const char *separator, int max_chunks)
{
	if (input)
	{
		int length = (int)strlen(input);
		int s_length = (int)strlen(separator);

		char **chunks = new char*[length];
		int count = 0, strlen = 0, end = length - 1;

		for (int i = end; i >= 0 && !(max_chunks > 0 && count >= max_chunks - 1); i--)
		{
			if (strncmp(&input[i], separator, s_length) == 0)
			{
				strlen = (end - i);

				if (strlen > 0 && input[i + s_length] != 0)
				{			
					chunks[count] = new char[strlen + 1]();
					strncpy(chunks[count], &input[i + s_length], strlen);
					count++;
				}

				end = i - s_length;
			}
		}

		if (end > 0)
		{
			end += s_length;
			chunks[count] = new char[end + 1]();
			strncpy(chunks[count], &input[0], end);
			count++;
		}


		// Перестроить массив результатов //
		char **result = new char*[count + 1]();
		memcpy(result, chunks, sizeof(char*) * count);

		delete[] chunks;

		return result;
	}

	return NULL;
}
#pragma endregion

#pragma region split_r(const wchar_t *input, const wchar_t *separator, int max_chunks)
wchar_t **split_r(const wchar_t *input, const wchar_t *separator, int max_chunks)
{
	if (input)
	{
		int length = (int)wcslen(input);
		int s_length = (int)wcslen(separator);

		wchar_t **chunks = new wchar_t*[length];
		int count = 0, strlen = 0, end = length - 1;

		for (int i = end; i >= 0 && !(max_chunks > 0 && count >= max_chunks - 1); i--)
		{
			if (wcsncmp(&input[i], separator, s_length) == 0)
			{
				strlen = end - i;

				if (strlen > 0 && input[i + s_length] != 0)
				{
					chunks[count] = new wchar_t[strlen + 1]();
					wcsncpy(chunks[count], &input[i + s_length], strlen);
					count++;
				}

				end = i - s_length;
			}
		}

		if (end > 0)
		{
			end += s_length;
			chunks[count] = new wchar_t[end + 1]();
			wcsncpy(chunks[count], &input[0], end);
			count++;
		}

		// Перестроить массив результатов //
		wchar_t **result = new wchar_t*[count + 1]();
		memcpy(result, chunks, sizeof(wchar_t*) * count);

		delete[] chunks;

		return result;
	}

	return NULL;
}
#pragma endregion

/*
// String : Split Quoted String
*/

#pragma region split_quotes(const char *input, const char separator, bool keep_quotes)
char** split_quotes(const char *input, const char separator, bool keep_quotes)
{
	if (&input && input)
	{
		size_t length = strlen(input);
		char **chunks = new char*[length];

		bool inQuotes = false;
		size_t count = 0, from = 0;

		for (size_t i = 0; i < length; i++)
		{
			if (input[i] == '"')
			{
				inQuotes = !inQuotes;
			}
			else if (input[i] == separator && !inQuotes)
			{
				size_t strlen = i - from;

				if (strlen > 0)
				{
					if (!keep_quotes && input[from] == '"' && input[i - 1] == '"')
					{
						from++; strlen -= 2;
					}

					chunks[count] = new char[strlen + 1]();
					strncpy(chunks[count], &input[from], strlen);
					count++;
				}

				from = i + 1;
			}
		}

		if (from < length)
		{
			size_t strlen = length - from;

			if (!keep_quotes && input[from] == L'"' && input[length - 1] == L'"')
			{
				from++; strlen -= 2;
			}

			chunks[count] = new char[strlen + 1]();
			strncpy(chunks[count], &input[from], strlen);
			count++;
		}

		// Перестроить массив результатов //
		char **result = new char*[count + 1]();
		memcpy(result, chunks, sizeof(char*) * count);

		delete[] chunks;

		return result;
	}

	return NULL;
}
#pragma endregion

#pragma region split_quotes(const wchar_t *input, const wchar_t separator, bool keep_quotes)
wchar_t** split_quotes(const wchar_t *input, const wchar_t separator, bool keep_quotes)
{	
	if (&input && input)
	{		
		size_t length = wcslen(input);		
		wchar_t **chunks = new wchar_t*[length];
		
		bool inQuotes = false;		
		size_t count = 0, from = 0;
				
		for (size_t i = 0; i < length; i++)
		{
			if (input[i] == L'"')
			{
				inQuotes = !inQuotes;
			}
			else if (input[i] == separator && !inQuotes)
			{
				size_t strlen = i - from;

				if (strlen > 0)
				{
					if (!keep_quotes && input[from] == L'"' && input[i-1] == L'"')
					{
						from++; strlen -= 2;
					}

					chunks[count] = new wchar_t[strlen + 1]();
					wcsncpy(chunks[count], &input[from], strlen);
					count++;
				}
				
				from = i + 1;
			}			
		}
		
		if (from < length)
		{
			size_t strlen = length - from;

			if (!keep_quotes && input[from] == L'"' && input[length - 1] == L'"')
			{
				from++; strlen -= 2;
			}

			chunks[count] = new wchar_t[strlen + 1]();
			wcsncpy(chunks[count], &input[from], strlen);
			count++;
		}		

		// Перестроить массив результатов //
		wchar_t **result = new wchar_t*[count + 1] ();
		memcpy(result, chunks, sizeof(wchar_t*) * count);

		delete[] chunks;

		return result;
	}

	return NULL;
}
#pragma endregion
