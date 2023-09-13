#ifndef _ULEGACY_MONO_H_
#define _ULEGACY_MONO_H_

#include "../main.hh"
#include "mono/api.h"
#include "../Features/strings.h"
struct ImageInfo
{	
	ImageInfo*		next;
	MonoImage*		image;
	MonoImage*		refimage;
};

struct AssemblyInfo
{
	AssemblyInfo*	next;
	MonoImage*		image;
	MonoAssembly*	assembly;
	unsigned int	crc;
};

struct AssemblyHook
{
	AssemblyHook*	next;
	void*			func;
	bool			initialized;
};

class Mono
{	
public:
	static MonoImage* mscorlib;
	static const char *rootDir;
	static MonoDomain *rootDomain;
	static MonoDomain *currentDomain;
	static AssemblyInfo* assemblies;

	static bool Initialize();
	static bool InstallHooks();

	static void RegisterRefonlyPreloadHook(MonoAssemblyPreLoadFunc func, void* user_data = NULL);
	static void RegisterPreloadHook(MonoAssemblyPreLoadFunc func, void* user_data = NULL);
	static void RegisterRefonlySearchHook(MonoAssemblySearchFunc func, void* user_data = NULL);
	static void RegisterSearchHook(MonoAssemblySearchFunc func, void* user_data = NULL);
	static void RegisterLoadHook(MonoAssemblyLoadFunc func, void* user_data = NULL);

	static ImageInfo* GetImages();
	static bool HasImage(const char* name);
	static bool HasImage(MonoImage* image);	

	static MonoImage* OpenImage(char* buffer, uint32_t length, MonoImageOpenStatus* status = NULL, gboolean ref_only = FALSE, gboolean protection = FALSE, const char* name = NULL);
	static MonoImage* OpenImage(const char* filepath, MonoImageOpenStatus* status = NULL, gboolean ref_only = FALSE, gboolean protection = FALSE);
	static MonoImage* OpenImage(const char* name, char** assemblies_path, MonoImageOpenStatus* status = NULL, gboolean ref_only = FALSE, gboolean protection = FALSE);

	static AssemblyInfo* GetAssemblies();

	static bool HasAssembly(const char* name, gboolean ref_only = FALSE);
	static MonoAssembly* GetAssembly(const char *name, gboolean ref_only = FALSE);
			
	static MonoAssembly* LoadAssembly(MonoImage* image, const char* path = NULL, MonoImageOpenStatus *status = NULL, gboolean ref_only = FALSE);
	static MonoAssembly* LoadAssembly(const char* filepath, MonoImageOpenStatus* status = NULL, gboolean ref_only = FALSE, gboolean protection = FALSE);
	static MonoAssembly* LoadAssembly(const char* name, char** assemblies_path, MonoImageOpenStatus* status = NULL, gboolean ref_only = FALSE, gboolean protection = FALSE);
	static MonoAssembly* LoadAssembly(char* buffer, uint32_t length, MonoImageOpenStatus* status = NULL, gboolean ref_only = FALSE, gboolean protection = FALSE, const char* name = NULL);

	// MonoType: GetType(fullname)
	#pragma region [static] GetType(const char *fullname)
	static MonoType* GetType(const char *fullname)
	{
		if (fullname)
		{
			MonoClass *klass = GetClass(fullname);

			if (klass)
			{
				return mono_class_get_type(klass);
			}
		}

		return NULL;
	}
	#pragma endregion

	// MonoClass: GetClass(fullname)
	#pragma region [static] GetClass(const char *fullname)
	static MonoClass* GetClass(const char *fullname)
	{
		MonoClass *result = NULL;
		if (fullname)
		{
			for (AssemblyInfo* info = Mono::GetAssemblies(); info; info = info->next)
			{
				MonoTableInfo *tdef = &info->image->tables[MONO_TABLE_TYPEDEF];

				for (guint i = 1; i < tdef->rows; ++i)
				{
					MonoClass *klass = mono_class_get(info->image, (i + 1) | MONO_TOKEN_TYPE_DEF);
					char *klass_fullname = GetClassFullName(klass);
					if (strcmp(klass_fullname, fullname) == 0)
					{
						return klass;
					}
				}
			}
		}

		return result;
	}
	#pragma endregion

	// MonoClass: GetClassFullName(MonoClass *klass)
	#pragma region [static] char* GetClassFullName(MonoClass *klass)
	static char* GetClassFullName(MonoClass *klass)
	{
		char *result = NULL;
		if (klass)
		{
			result = (char*)mono_class_get_name(klass);
			char *klass_ns = (char*)mono_class_get_namespace(klass);

			MonoClass *parent = mono_class_get_nesting_type(klass);
			
			if (parent)
			{				
				do
				{
					result = formatf("%s.%s", mono_class_get_name(parent), result);

					klass_ns = (char*)mono_class_get_namespace(parent);
					if (klass_ns && strlen(klass_ns) > 0)
					{
						result = formatf("%s.%s", klass_ns, result);
						break;
					}					
				} 				
				
				while (parent = mono_class_get_nesting_type(parent));
			}
			else if (klass_ns && strlen(klass_ns) > 0)
			{				
				result = formatf("%s.%s", klass_ns, result);				
			}			
		}
		return result;
	}
	#pragma endregion

	// MonoClass: GetClass(MonoClass, classname)
	#pragma region [static] MonoClass* GetClass(MonoClass* parent, const char *_classname)
	/*
	static MonoClass* GetClass(MonoClass* parent, const char *_classname)
	{
		if (_classname && parent)
		{
			MonoClass *result = NULL; void *iterate = NULL;

			while ((result = mono_class_get_nested_types(parent, &iterate)))
			{
				if (strcmp(mono_class_get_name(result), _classname) == 0)
				{
					return result;
				}
			}
		}
		return NULL;
	}
	*/
	#pragma endregion

	// GetClassField : GetField(fullname, params)
	#pragma region [static] MonoClassField* GetField(const char *_fullname)
	static MonoClassField* GetField(const char *_fullname)
	{
		char **chunks = split_r(_fullname, '.', 2);
		char *_field_name = *chunks++;
		char *_class_name = *chunks;		

		if (_class_name && _field_name)
		{
			MonoClass *_class = GetClass(_class_name);
			
			if (_class)
			{
				return mono_class_get_field_from_name(_class, _field_name);
			}
		}
		return NULL;
	}
	#pragma endregion

	// MonoProperty: GetProperty(fullname, params)
	#pragma region [static] MonoProperty* GetProperty(const char *_fullname)
	static MonoProperty* GetProperty(const char *_fullname)
	{
		char **chunks = split_r(_fullname, '.', 2);
		char *_property_name = *chunks++;
		char *_class_name = *chunks;

		//wofstream log("D:/methods.txt", ios_base::app); log << "[Native] GetProperty: _property_name = " << _property_name << ", _class_name = " << _class_name << endl; log.close();

		if (_class_name && _property_name)
		{
			MonoClass *_class = GetClass(_class_name);
			
			//wofstream log("D:/methods.txt", ios_base::app); log << "[Native] GetProperty: _class = " << _class << ", _class_name = " << _class_name << endl; log.close();

			if (_class)
			{
				return mono_class_get_property_from_name(_class, _property_name);
			}
		}
		return NULL;
	}
	#pragma endregion

	// MonoMethod: GetMethod(fullname, params)
	#pragma region [static] MonoMethod* GetMethod(const char *_fullname)
	static MonoMethod* GetMethod(const char *_fullname)
	{
		char **chunks = split_r(*split(_fullname, '(', 2), '.', 2);
		char *_invoke_name = *chunks++;
		char *_invoke_class = trim(*chunks, ".");

		if (_invoke_class && _invoke_name)
		{
			MonoClass *_class = GetClass(_invoke_class);
			
			if (_class)
			{
				MonoMethod *_method = NULL; void *iterate = NULL;

				while ((_method = mono_class_get_methods(_class, &iterate)))
				{	
					char* method_fullname = GetMethodFullName(_method);

					if (strcmp(method_fullname, _fullname) == 0)
					{
						return _method;
					}
				}
			}
		}
		return NULL;
	}
	#pragma endregion

	// MonoClass: GetMethodFullName(MonoMethod *method)
	#pragma region [static] char* GetMethodFullName(MonoMethod *method)
	static char* GetMethodFullName(MonoMethod *method)
	{
		if (method)
		{			
			return formatf("%s.%s%s(%s)", GetClassFullName(mono_method_get_class(method)), mono_method_get_name(method), GetGenericParameters(method), GetMethodParameters(method));
		}
		return NULL;
	}
	#pragma endregion

	// MonoMethod: GetMethodParameters(MonoMethod)
	#pragma region [static] char* GetGenericParameters(MonoMethod *_method)
	static const char* GetGenericParameters(MonoMethod* _method)
	{
		const char* result = new char[1](/* null-terminated char */);

		if (_method)
		{
			MonoMethodSignature *sig = mono_method_signature(_method);			

			if (sig->generic_param_count > 0)
			{
				if (sig->generic_param_count == 1)
				{
					result = "T";
				}
				else if (sig->generic_param_count > 1)
				{
					for (size_t i = 0; i < sig->generic_param_count; i++)
					{
						if (i > 0)
						{
							result = formatf("%s, T%i", result, i);
						}
						else
						{
							result = formatf("T%i", i);
						}
					}
				}
				return formatf("<%s>", result);
			}
		}

		return result;
	}
	#pragma endregion

	// MonoMethod: GetMethodParameters(MonoMethod)
	#pragma region [static] char* GetMethodParameters(MonoMethod *_method)
	static char* GetMethodParameters(MonoMethod *_method)
	{
		char *result = new char[1](/* null-terminated char */);

		if (_method)
		{
			MonoMethodSignature *sig = mono_method_signature(_method);
			MonoClass *_parent_class = NULL;

			for (int i = 0; i < sig->param_count; i++)
			{
				MonoClass *_param_class = mono_class_from_mono_type(sig->params[i]);								
				char* _param_name = (char*)mono_class_get_name(_param_class);				

				char* _name_space = (char*)mono_class_get_namespace(_param_class);				
				if (_name_space && strlen(_name_space) > 0)
				{
					_param_name = formatf("%s.%s", _name_space, _param_name);
				}
	
				if (i > 0)
				{
					result = formatf("%s, %s", result, _param_name);
				}
				else
				{
					result = _param_name;
				}
			}
		}

		return result;
	}
	#pragma endregion


	// MonoClassField -> GetValue(MonoClassField, obj)
	#pragma region [static] void* GetValue(MonoClassField *mono_field, void *obj = NULL)
	static void* GetValue(MonoClassField *mono_field, void *obj = NULL)
	{
		if (mono_field)
		{
			MonoObject* result = NULL;
			MonoType *return_type = mono_field->type;
			uint32_t field_flags = mono_field_get_flags(mono_field);

			if (field_flags & MONO_FIELD_ATTR_HAS_DEFAULT)
			{
				if (mono_field->type->attrs & FIELD_ATTRIBUTE_LITERAL || mono_field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA)
				{
					mono_field_static_get_value(mono_class_vtable(currentDomain, mono_field->parent), mono_field, &result);

					if (result && return_type->type == MONO_TYPE_STRING)
					{
						return mono_string_to_utf8((MonoString*)result);
					}

					return result;
				}
			}

			if (field_flags & MONO_FIELD_ATTR_STATIC)
			{
				MonoVTable* vtable = mono_class_vtable(currentDomain, mono_field->parent);

				if (vtable)
				{
					mono_runtime_class_init(vtable);
					mono_field_static_get_value(vtable, mono_field, &result);
				}
			}
			else if (obj)
			{
				mono_field_get_value((MonoObject*)obj, mono_field, &result);
			}

			if (result && return_type->type == MONO_TYPE_STRING)
			{
				return mono_string_to_utf8((MonoString*)result);
			}

			return result;
		}

		return NULL;
	}
	#pragma endregion

	#pragma region [static] T GetValue<T>(MonoClassField *mono_field, void *obj = NULL)
	template <class T> static T GetValue(MonoClassField *mono_field, void *obj = NULL)
	{
		if (mono_field)
		{
			MonoObject* result = NULL;
			MonoType *return_type = mono_field->type;
			uint32_t field_flags = mono_field_get_flags(mono_field);

			if (field_flags & MONO_FIELD_ATTR_HAS_DEFAULT)
			{
				if (mono_field->type->attrs & FIELD_ATTRIBUTE_LITERAL || mono_field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA)
				{
					mono_field_static_get_value(mono_class_vtable(currentDomain, mono_field->parent), mono_field, &result);

					if (result && return_type->type == MONO_TYPE_STRING)
					{
						return (T)mono_string_to_utf8((MonoString*)result);
					}

					return (T)result;
				}
			}

			if (field_flags & MONO_FIELD_ATTR_STATIC)
			{
				MonoVTable* vtable = mono_class_vtable(currentDomain, mono_field->parent);

				if (vtable)
				{
					mono_runtime_class_init(vtable);
					mono_field_static_get_value(vtable, mono_field, &result);
				}
			}
			else if (obj)
			{
				mono_field_get_value((MonoObject*)obj, mono_field, &result);
			}

			if (result && return_type->type == MONO_TYPE_STRING)
			{
				return (T)mono_string_to_utf8((MonoString*)result);
			}

			return (T)result;
		}

		return NULL;
	}
	#pragma endregion

	// MonoClassField -> SetValue(MonoClassField, obj, value)
	#pragma region [static] void SetValue(MonoClassField *mono_field, void *obj = NULL, void* value = NULL)
	static void SetValue(MonoClassField *mono_field, void *obj = NULL, void* value = NULL)
	{
		if (mono_field)
		{
			MonoType *return_type = mono_field->type;

			if (return_type->type == MONO_TYPE_STRING && value != NULL)
			{
				value = mono_string_new(currentDomain, (char*)value);
			}

			if (mono_field_get_flags(mono_field) & MONO_FIELD_ATTR_STATIC)
			{
				MonoVTable* vtable = mono_class_vtable(currentDomain, mono_field->parent);

				if (vtable)
				{
					mono_runtime_class_init(vtable);
					mono_field_static_set_value(vtable, mono_field, value);					
				}
			}
			else if (obj)
			{
				mono_field_set_value((MonoObject*)obj, mono_field, value);
			}
		}
	}
	#pragma endregion


	// MonoProperty -> GetValue(MonoProperty, obj, value)
	#pragma region [static] void* GetValue(MonoProperty *mono_property, void *obj = NULL, void **params = NULL)
	static void* GetValue(MonoProperty *mono_property, void *obj = NULL, void **params = NULL)
	{
		if (mono_property)
		{
			MonoObject *result = NULL;
			MonoType *return_type = NULL;

			MonoMethodSignature *signature = mono_method_signature(mono_property->get);
			if (signature) return_type = mono_signature_get_return_type(signature);

			if (mono_property->get && return_type && !MONO_TYPE_IS_VOID(return_type))
			{
				MonoObject *exception;
				result = mono_property_get_value(mono_property, obj, params, &exception);

				if (!exception && result)
				{
					if (return_type->type == MONO_TYPE_STRING)
					{
						return mono_string_to_utf8((MonoString*)result);						
					}
					else if (return_type->type >= MONO_TYPE_BOOLEAN && return_type->type < MONO_TYPE_STRING)
					{
						return mono_object_unbox(result);
					}
				}

				return result;
			}			
		}

		return NULL;
	}
	#pragma endregion

	#pragma region [static] T GetValue<T>(MonoProperty *mono_property, void *obj = NULL, void **params = NULL)
	template <class T> static T GetValue(MonoProperty *mono_property, void *obj = NULL, void **params = NULL)
	{
		if (mono_property)
		{
			MonoObject *result = NULL;
			MonoType *return_type = NULL;

			MonoMethodSignature *signature = mono_method_signature(mono_property->get);
			if (signature) return_type = mono_signature_get_return_type(signature);

			if (mono_property->get && return_type && !MONO_TYPE_IS_VOID(return_type))
			{
				MonoObject *exception;
				result = mono_property_get_value(mono_property, obj, params, &exception);

				if (!exception && result)
				{
					if (return_type->type == MONO_TYPE_STRING)
					{
						return (T)mono_string_to_utf8((MonoString*)result);						
					}
					else if (return_type->type >= MONO_TYPE_BOOLEAN && return_type->type < MONO_TYPE_STRING)
					{
						return *(reinterpret_cast<T*>(mono_object_unbox(result)));
					}
				}

				return (T)result;
			}			
		}

		return NULL;
	}
	#pragma endregion

	// MonoProperty -> SetValue(MonoProperty, obj, value)
	#pragma region [static] void SetValue(MonoProperty *mono_property, void *obj = NULL, void* value = NULL)
	static void SetValue(MonoProperty *mono_property, void *obj = NULL, void* value = NULL)
	{
		if (mono_property)
		{
			if (mono_property && mono_property->set)
			{
				MonoMethodSignature *signature = mono_method_signature(mono_property->set);

				if (signature && signature->param_count > 0)
				{
					MonoType *paramType = signature->params[0];

					if (paramType->type == MONO_TYPE_STRING && value != NULL)
					{
						value = mono_string_new(currentDomain, (char*)value);
					}
					else if (paramType->type > MONO_TYPE_BOOLEAN && paramType->type < MONO_TYPE_STRING)
					{
						if (paramType->byref || mono_property->parent->element_class->valuetype)
						{
							value = mono_value_box(currentDomain, mono_property->parent, value);
						}
					}

					mono_property_set_value(mono_property, obj, &value, NULL);
				}
			}
		}
	}
	#pragma endregion


	// MonoClassField / MonoProperty -> GetValue(fullname, obj, params)
	#pragma region [static] void* GetValue<T>(const char *fullname, void *obj = NULL, void **params = NULL)
	static void* GetValue(const char *fullname, void *obj = NULL, void **params = NULL)
	{
		char **chunks = split_r(fullname, '.', 2);
		char *_fieldname = *chunks++;
		char *_classname = *chunks;

		if (_classname && _fieldname)
		{
			MonoClass *_class = GetClass(_classname);

			if (_class)
			{
				MonoClassField *mono_field = mono_class_get_field_from_name(_class, _fieldname);
				if (mono_field) return GetValue(mono_field, obj);

				MonoProperty* mono_property = mono_class_get_property_from_name(_class, _fieldname);
				if (mono_property) return GetValue(mono_property, obj, params);
			}
		}

		return NULL;
	}
	#pragma endregion

	#pragma region [static] T GetValue<T>(const char *_fullname, void *obj = NULL, void **params = NULL)
	template <class T> static T GetValue(const char *_fullname, void *obj = NULL, void **params = NULL)
	{
		char **chunks = split_r(_fullname, '.', 2);		
		char *_fieldname = *chunks++;
		char *_classname = *chunks;

		if (_classname && _fieldname)
		{
			MonoClass *_class = GetClass(_classname);

			if (_class)
			{
				MonoClassField *mono_field = mono_class_get_field_from_name(_class, _fieldname);
				if (mono_field) return GetValue<T>(mono_field, obj);

				MonoProperty* mono_property = mono_class_get_property_from_name(_class, _fieldname);
				if (mono_property) return GetValue<T>(mono_property, obj, params);
			}
		}

		return NULL;
	}
	#pragma endregion

	// MonoClassField / MonoProperty -> SetValue(fullname, obj, value)
	#pragma region [static] bool SetValue(const char *_fullname, void *obj = NULL, T value = NULL)
	template<class T> static bool SetValue(const char *_fullname, void *obj = NULL, T value = NULL)
	{				
		char **chunks = split_r(_fullname, '.', 2);
		char *_fieldname = *chunks++;
		char *_classname = *chunks;

		//Console.Log("SetValue: %s", _classname);
		//Console.Log("SetValue: %s", _fieldname);
		//Console.Log("SetValue->obj: %p", obj);

		if (_classname && _fieldname)
		{
			MonoClass *_class = GetClass(_classname);

			//Console.Log("_class: %p", _class);

			if (_class)
			{
				#pragma region [ SetValue of Field ]
				MonoClassField *_field = mono_class_get_field_from_name(_class, _fieldname);

				//Console.Log("_field: %p", _field);

				if (_field)
				{
					void *monoValue = value;
					//Console.Log("_field: %s", mono_field_get_name(_field));
					//Console.Log("_field->type[%p] %s", _field->type, mono_type_get_name(_field->type));										

					MonoType *return_type = _field->type;
					//Console.Log("return_type->type: %i", return_type->type);

					if (return_type->type == MONO_TYPE_STRING)
					{
						if (typeid(T) == typeid(wchar_t*))
						{
							//Console.Log("field: value to monostring_utf16");
							monoValue = mono_string_new_utf16(currentDomain, (mono_unichar2*)value, wcslen((wchar_t*)value));
						}
						else if (typeid(T) == typeid(char*))
						{							
							//Console.Log("field: value to monostring");
							monoValue = mono_string_new(currentDomain, (char*)value);
						}
					}					

					if (mono_field_get_flags(_field) & MONO_FIELD_ATTR_STATIC)
					{						
						//Console.Log("SetValue(static)->_field->parent: %p", _field->parent);
						//Console.Log("SetValue(static)->_field->parent: %s", mono_class_get_name(_field->parent));

						MonoVTable* vtable = mono_class_vtable(currentDomain, _class);

						if (vtable)
						{							
							mono_runtime_class_init(vtable);
							mono_field_static_set_value(vtable, _field, monoValue);
							return true;
						}
					}
					else if (obj)
					{
						//Console.Log("SetValue(non-static)->obj: %p", obj);
						mono_field_set_value((MonoObject*)obj, _field, monoValue);
						return true;
					}				

					return false;
				}
				#pragma endregion

				#pragma region [ SetValue of Property ]
				MonoProperty* _property = mono_class_get_property_from_name(_class, _fieldname);

				//Console.Log("_property: %p -> set: %p", _property, _property->set);

				if (_property && _property->set)
				{
					void *monoValue = value;

					//Console.Log("_property: %s", mono_property_get_name(_property));

					MonoMethodSignature *signature = mono_method_signature(_property->set);									

					//Console.Log("signature: %p", signature);
					//Console.Log("signature->param_count: %p", signature->param_count);
					
					if (signature && signature->param_count > 0)
					{
						MonoType *paramType = signature->params[0];

						//Console.Log("signature->paramType[0]: %p", paramType);

						if (paramType->type == MONO_TYPE_STRING)
						{
							if (typeid(T) == typeid(wchar_t*))
							{
								//Console.Log("property: value to monostring_utf16");
								monoValue = mono_string_new_utf16(currentDomain, (mono_unichar2*)value, wcslen((wchar_t*)value));
							}
							else if (typeid(T) == typeid(char*))
							{
								//Console.Log("property: value to monostring");
								monoValue = mono_string_new(currentDomain, (char*)value);
							}
						}
						else if (paramType->type > MONO_TYPE_BOOLEAN && paramType->type < MONO_TYPE_STRING)
						{
							if (paramType->byref || _class->element_class->valuetype)
							{
								//Console.Log("property: mono_value_box(value)");
								monoValue = mono_value_box(currentDomain, _class, monoValue);
							}
						}

						MonoObject *exception;
						mono_property_set_value(_property, obj, &monoValue, &exception);

						//Console.Log("value: %p, exception: %p", value, exception);

						return true;
					}
				}
				#pragma endregion
			}
		}

		return false;
	}
	#pragma endregion

	// MonoMethod : Invoke(MonoMethod, obj, params)
	#pragma region [static] void* Invoke(MonoMethod *mono_method, void *obj = NULL, void **params = NULL)
	static void* Invoke(MonoMethod *mono_method, void *obj = NULL, void **params = NULL)
	{
		if (mono_method)
		{			
			MonoObject *result = NULL;
			MonoObject *exception = NULL;
			MonoType *return_type = mono_method->signature->ret;

			result = mono_runtime_invoke(mono_method, obj, params, &exception);
			
			if (!exception)
			{
				if (result && return_type->type != MONO_TYPE_VOID)
				{
					if (return_type->type == MONO_TYPE_STRING)
					{
						return mono_string_to_utf8((MonoString*)result);
					}
					else if (return_type->type >= MONO_TYPE_BOOLEAN && return_type->type < MONO_TYPE_STRING)
					{
						return mono_object_unbox(result);
					}
					return result;
				}
			}
		}
		return NULL;
	}
	#pragma endregion

	#pragma region [static] T Invoke<T>(MonoMethod *mono_method, void *obj = NULL, void **params = NULL)
	template <class T> static T Invoke(MonoMethod *mono_method, void *obj = NULL, void **params = NULL)
	{
		if (mono_method)
		{						
			MonoObject *result = NULL;
			MonoObject *exception = NULL;
			MonoType *return_type = mono_method->signature->ret;

			result = mono_runtime_invoke(mono_method, obj, params, &exception);

			if (!exception && result && return_type->type != MONO_TYPE_VOID)
			{
				if (return_type->type == MONO_TYPE_STRING)
				{
					return (T)mono_string_to_utf8((MonoString*)result);
				}
				else if (return_type->type >= MONO_TYPE_BOOLEAN && return_type->type < MONO_TYPE_STRING)
				{
					return *(reinterpret_cast<T*>(mono_object_unbox(result)));
				}				

				return (T)result;
			}			
		}
		return NULL;
	}
	#pragma endregion

	// MonoMethod : Invoke(fullname, obj, params)
	#pragma region [static] void* Invoke(const char *fullname, void *obj = NULL, void **params = NULL)
	static void* Invoke(const char *fullname, void *obj = NULL, void **params = NULL)
	{
		MonoMethod *method = GetMethod(fullname);
		
		if (method)
		{
			return Invoke(method, obj, params);
		}

		return NULL;
	}
	#pragma endregion

	#pragma region [static] T Invoke<T>(const char *fullname, void *obj = NULL, void **params = NULL)
	template <class T> static T Invoke(const char *fullname, void *obj = NULL, void **params = NULL)
	{
		MonoMethod *method = GetMethod(fullname);

		if (method)
		{
			return Invoke<T>(method, obj, params);
		}

		return NULL;
	}
	#pragma endregion	
};

#endif