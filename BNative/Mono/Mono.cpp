#include "Mono.h"

/*
// Variables //
*/

MonoImage* Mono::mscorlib = NULL;
const char* Mono::rootDir = NULL;
MonoDomain* Mono::rootDomain = NULL;
MonoDomain* Mono::currentDomain = NULL;
AssemblyInfo* Mono::assemblies = NULL;

MonoImageOpenStatus def_status = MONO_IMAGE_OK;

/*
* Internal Variables
*/

static ImageInfo* images = NULL;

static bool refonly_preload_hook_initialized = FALSE;
static bool preload_hook_initialized = FALSE;
static bool refonly_search_hook_initialized = FALSE;
static bool search_hook_initialized = FALSE;
static bool load_hook_initialized = FALSE;

static AssemblyHook* assembly_refonly_preload_hook = NULL;
static AssemblyHook* assembly_preload_hook = NULL;
static AssemblyHook* assembly_refonly_search_hook = NULL;
static AssemblyHook* assembly_search_hook = NULL;
static AssemblyHook* assembly_load_hook = NULL;

/*
 * Internal Functions
*/

#pragma region [ internal static ] func_assembly_refonly_preload(MonoAssemblyName *aname, char **assemblies_path, void *user_data)
MonoAssembly* func_assembly_refonly_preload(MonoAssemblyName* aname, char** assemblies_path, void* user_data)
{
    refonly_preload_hook_initialized = true;

    if (assembly_refonly_preload_hook)
    {
        for (AssemblyHook* hook = assembly_refonly_preload_hook; hook; hook = hook->next)
        {
            MonoAssemblyPreLoadFunc func = (MonoAssemblyPreLoadFunc)hook->func;

            hook->initialized = (func != NULL);

            if (hook->initialized)
            {
                MonoAssembly* result = func(aname, assemblies_path, user_data);
                if (result) return result;
            }
        }
    }

    return NULL;
}
#pragma endregion

#pragma region [ internal static ] func_assembly_preload(MonoAssemblyName *aname, char **assemblies_path, void *user_data)
MonoAssembly* func_assembly_preload(MonoAssemblyName* aname, char** assemblies_path, void* user_data)
{
    preload_hook_initialized = true;

    if (assembly_preload_hook)
    {
        for (AssemblyHook* hook = assembly_preload_hook; hook; hook = hook->next)
        {
            MonoAssemblyPreLoadFunc func = (MonoAssemblyPreLoadFunc)hook->func;

            hook->initialized = (func != NULL);

            if (hook->initialized)
            {
                MonoAssembly* result = func(aname, assemblies_path, user_data);
                if (result) return result;
            }
        }
    }

    return NULL;
}
#pragma endregion

#pragma region [ internal static ] func_assembly_refonly_search(MonoAssemblyName *aname, void* user_data)
MonoAssembly* func_assembly_refonly_search(MonoAssemblyName* aname, void* user_data)
{
    refonly_search_hook_initialized = true;

    if (assembly_refonly_search_hook)
    {
        for (AssemblyHook* hook = assembly_refonly_search_hook; hook; hook = hook->next)
        {
            MonoAssemblySearchFunc func = (MonoAssemblySearchFunc)hook->func;

            hook->initialized = (func != NULL);

            if (hook->initialized)
            {
                MonoAssembly* result = func(aname, user_data);
                if (result) return result;
            }
        }
    }

    return NULL;
}
#pragma endregion

#pragma region [ internal static ] func_assembly_search(MonoAssemblyName *aname, void* user_data)
MonoAssembly* func_assembly_search(MonoAssemblyName* aname, void* user_data)
{
    search_hook_initialized = true;

    if (assembly_search_hook)
    {
        for (AssemblyHook* hook = assembly_search_hook; hook; hook = hook->next)
        {
            MonoAssemblySearchFunc func = (MonoAssemblySearchFunc)hook->func;

            hook->initialized = (func != NULL);

            if (hook->initialized)
            {
                MonoAssembly* result = func(aname, user_data);
                if (result) return result;
            }
        }
    }

    return NULL;
}
#pragma endregion

#pragma region [ internal static ] func_assembly_load(MonoAssembly *assembly, void *user_data)
void func_assembly_load(MonoAssembly* assembly, void* user_data)
{
    load_hook_initialized = true;

    AssemblyInfo* info = new AssemblyInfo();
    info->crc = CRC32(assembly->image->raw_data, assembly->image->raw_data_len).result;
    info->image = assembly->image;
    info->assembly = assembly;
    info->next = Mono::assemblies;
    Mono::assemblies = info;

    if (assembly_load_hook)
    {
        for (AssemblyHook* hook = assembly_load_hook; hook; hook = hook->next)
        {
            MonoAssemblyLoadFunc func = (MonoAssemblyLoadFunc)hook->func;

            hook->initialized = (func != NULL);

            if (hook->initialized)
            {
                func(assembly, user_data);
            }
        }
    }
}
#pragma endregion

#pragma region [ internal static ] register_image(MonoImage *image)
void register_image(MonoImage* image)
{
    if (images)
    {
        for (ImageInfo* info = images; info; info = info->next)
        {
            if (info->image == image) return;
        }
    }

    ImageInfo* imageInfo = new ImageInfo();
    imageInfo->image = image;
    imageInfo->next = images;
    images = imageInfo;
}
#pragma endregion

#pragma region [ internal static ] unregister_image(MonoImage *image)
void unregister_image(MonoImage* image)
{
    if (images)
    {
        if (images->image == image)
        {
            images = images->next;
            return;
        }

        for (ImageInfo* info = images; info; info = info->next)
        {
            if (info->next->image == image)
            {
                info->next = info->next->next;
                return;
            }
        }
    }
}
#pragma endregion

/*
 * Public Functions
*/

#pragma region Mono::Initialize()
bool Mono::Initialize()
{
    if (mono_domain_get)
    {
        mscorlib = mono_get_corlib();
        currentDomain = mono_domain_get();

        if (mono_get_root_domain)
        {
            rootDomain = mono_get_root_domain();
        }

        if (mono_assembly_getrootdir)
        {
            rootDir = mono_assembly_getrootdir();
        }

        if (GetAssemblies() != NULL)
        {
            for (AssemblyInfo* info = assemblies; info; info = info->next)
            {
                if (!mscorlib && strcmp(info->image->assembly_name, "mscorlib") == 0)
                {
                    mscorlib = info->image;
                }

                ImageInfo* imageInfo = new ImageInfo();
                imageInfo->image = info->image;
                imageInfo->next = images;
                images = imageInfo;
            }
            return true;
        }
    }
    return false;
}
#pragma endregion

#pragma region Mono::InstallHooks()
bool Mono::InstallHooks()
{
    // Install hooks //
    if (mono_install_assembly_refonly_preload_hook)
    {
        mono_install_assembly_refonly_preload_hook(func_assembly_refonly_preload, NULL);
        refonly_preload_hook_initialized = true;
    }

    if (mono_install_assembly_preload_hook)
    {
        mono_install_assembly_preload_hook(func_assembly_preload, NULL);
        preload_hook_initialized = true;
    }

    if (mono_install_assembly_refonly_search_hook)
    {
        mono_install_assembly_refonly_search_hook(func_assembly_refonly_search, NULL);
        refonly_search_hook_initialized = true;
    }

    if (mono_install_assembly_search_hook)
    {
        mono_install_assembly_search_hook(func_assembly_search, NULL);
        search_hook_initialized = true;
    }

    if (mono_install_assembly_load_hook)
    {
        mono_install_assembly_load_hook(func_assembly_load, NULL);
        load_hook_initialized = true;
    }
    return (preload_hook_initialized && search_hook_initialized && load_hook_initialized);
}
#pragma endregion

#pragma region Mono::RegisterRefonlyPreloadHook(MonoAssemblyPreLoadFunc func, void* user_data)
void Mono::RegisterRefonlyPreloadHook(MonoAssemblyPreLoadFunc func, void* user_data)
{
    if (mono_install_assembly_refonly_preload_hook)
    {
        if (assembly_refonly_preload_hook)
        {
            for (AssemblyHook* hook = assembly_refonly_preload_hook; hook; hook = hook->next)
            {
                if (hook->func == func && hook->initialized) return;
            }
        }
        AssemblyHook* hook = new AssemblyHook();
        hook->next = assembly_refonly_preload_hook;
        hook->func = func;
        hook->initialized = true;
        assembly_refonly_preload_hook = hook;
    }
}
#pragma endregion

#pragma region Mono::RegisterPreloadHook(MonoAssemblyPreLoadFunc func, void* user_data)
void Mono::RegisterPreloadHook(MonoAssemblyPreLoadFunc func, void* user_data)
{
    if (mono_install_assembly_preload_hook)
    {
        if (assembly_preload_hook)
        {
            for (AssemblyHook* hook = assembly_preload_hook; hook; hook = hook->next)
            {
                if (hook->func == func && hook->initialized) return;
            }
        }
        AssemblyHook* hook = new AssemblyHook();
        hook->next = assembly_preload_hook;
        hook->func = func;
        hook->initialized = true;
        assembly_preload_hook = hook;
    }
}
#pragma endregion

#pragma region Mono::RegisterRefonlySearchHook(MonoAssemblySearchFunc func, void* user_data)
void Mono::RegisterRefonlySearchHook(MonoAssemblySearchFunc func, void* user_data)
{
    if (mono_install_assembly_refonly_search_hook)
    {
        if (assembly_refonly_search_hook)
        {
            for (AssemblyHook* hook = assembly_refonly_search_hook; hook; hook = hook->next)
            {
                if (hook->func == func && hook->initialized) return;
            }
        }
        AssemblyHook* hook = new AssemblyHook();
        hook->next = assembly_refonly_search_hook;
        hook->func = func;
        hook->initialized = true;
        assembly_refonly_search_hook = hook;
    }
}
#pragma endregion

#pragma region Mono::RegisterSearchHook(MonoAssemblySearchFunc func, void* user_data)
void Mono::RegisterSearchHook(MonoAssemblySearchFunc func, void* user_data)
{
    if (mono_install_assembly_search_hook)
    {
        if (assembly_search_hook)
        {
            for (AssemblyHook* hook = assembly_search_hook; hook; hook = hook->next)
            {
                if (hook->func == func && hook->initialized) return;
            }
        }
        AssemblyHook* hook = new AssemblyHook();
        hook->next = assembly_search_hook;
        hook->func = func;
        hook->initialized = true;
        assembly_search_hook = hook;
    }
}
#pragma endregion

#pragma region Mono::RegisterLoadHook(MonoAssemblyLoadFunc func, void* user_data)
void Mono::RegisterLoadHook(MonoAssemblyLoadFunc func, void* user_data)
{
    if (mono_install_assembly_load_hook)
    {
        if (assembly_load_hook)
        {
            for (AssemblyHook* hook = assembly_load_hook; hook; hook = hook->next)
            {
                if (hook->func == func && hook->initialized) return;
            }
        }
        AssemblyHook* hook = new AssemblyHook();
        hook->next = assembly_load_hook;
        hook->func = func;
        hook->initialized = true;
        assembly_load_hook = hook;
    }
}
#pragma endregion

// Image

#pragma region Mono::GetImages()
ImageInfo* Mono::GetImages()
{
    return images;
}
#pragma endregion

#pragma region Mono::HasImage(const char* name)
bool Mono::HasImage(const char* name)
{
    if (images)
    {
        for (ImageInfo* info = images; info; info = info->next)
        {
            if (info && info->image && strcmp(info->image->assembly_name, name) == 0)
            {
                return true;
            }
        }
    }
    return false;
}
#pragma endregion

#pragma region Mono::HasImage(MonoImage* image)
bool Mono::HasImage(MonoImage* image)
{
    if (image)
    {
        return Mono::HasImage(image->assembly_name);
    }
    return FALSE;
}
#pragma endregion

// Load Image

#pragma region Mono::OpenImage(char* buffer, uint32_t length, MonoImageOpenStatus* status, gboolean ref_only, gboolean protection, const char* name)
MonoImage* Mono::OpenImage(char* buffer, uint32_t length, MonoImageOpenStatus* status, gboolean ref_only,
                           gboolean protection, const char* name)
{
    if (*buffer && buffer && length > 0)
    {
        if (!status) status = &def_status;
        else *status = MONO_IMAGE_OK;

        MonoImage* image = mono_image_open_from_data_with_name(buffer, length, TRUE, status, ref_only, name);

        if (image != NULL)
        {
            if (name == NULL || strlen(name) == 0)
            {
                name = strconcat(mono_assembly_getrootdir(), PATH_SEPARATOR, image->module_name, NULL);
                if (_access(name, 0) != -1) image->name = (char*)name;
            }

            if (protection)
            {
                IMAGE_DOS_HEADER image_dos_header;
                memcpy(&image_dos_header, image->raw_data, sizeof(image_dos_header));
                if (image_dos_header.e_magic != IMAGE_DOS_SIGNATURE)
                {
                    *status = MONO_IMAGE_IMAGE_INVALID;
                    mono_image_close(image);
                    return NULL;
                }

                IMAGE_NT_HEADERS image_nt_headers;
                memcpy(&image_nt_headers, image->raw_data + image_dos_header.e_lfanew, sizeof(image_nt_headers));
                if (image_nt_headers.Signature != IMAGE_NT_SIGNATURE)
                {
                    *status = MONO_IMAGE_IMAGE_INVALID;
                    mono_image_close(image);
                    return NULL;
                }

                DWORD dwProtection;
                size_t szProtection = sizeof(IMAGE_NT_HEADERS) + image_dos_header.e_lfanew;
                szProtection += image_nt_headers.FileHeader.NumberOfSections * sizeof(IMAGE_SECTION_HEADER);

                if (!VirtualProtect(image->raw_data, szProtection, PAGE_EXECUTE_READWRITE, &dwProtection))
                {
                    mono_image_close(image);
                    return NULL;
                }

                RtlZeroMemory(image->raw_data, szProtection);

                if (!VirtualProtect(image->raw_data, szProtection, dwProtection, &dwProtection))
                {
                    mono_image_close(image);
                    return NULL;
                }
            }

            // Register image //
            if (!image->ref_only)
            {
                register_image(image);
            }

            return image;
        }
    }

    *status = MONO_IMAGE_IMAGE_INVALID;
    return NULL;
}
#pragma endregion

#pragma region Mono::OpenImage(const char* fullpath, MonoImageOpenStatus* status, gboolean ref_only, gboolean protection)
MonoImage* Mono::OpenImage(const char* filepath, MonoImageOpenStatus* status, gboolean ref_only, gboolean protection)
{
    char* buffer = NULL;
    size_t length = file_readallbytes(filepath, buffer);

    if (length > 0)
    {
        return Mono::OpenImage(buffer, length, status, ref_only, protection, filepath);
    }

    if (status) *status = MONO_IMAGE_IMAGE_INVALID;

    return NULL;
}
#pragma endregion

#pragma region Mono::OpenImage(const char* name, char** assemblies_path, MonoImageOpenStatus *status, gboolean ref_only, gboolean protection)
MonoImage* Mono::OpenImage(const char* name, char** assemblies_path, MonoImageOpenStatus* status, gboolean ref_only,
                           gboolean protection)
{
    if (assemblies_path != NULL)
    {
        if (!status) status = &def_status;
        else *status = MONO_IMAGE_OK;

        while (*assemblies_path)
        {
            char* filepath = strconcat(*assemblies_path++, PATH_SEPARATOR, name, ".dll", NULL);

            MonoImage* image = Mono::OpenImage(filepath, status, ref_only, protection);

            if (image && *status == MONO_IMAGE_OK)
            {
                return image;
            }
        }
    }

    return NULL;
}
#pragma endregion

// Assembly

#pragma region [ internal static ] func_assembly_foreach(void *data, void *user_data)
void func_assembly_foreach(MonoAssembly* assembly, void* user_data)
{
    if (assembly)
    {
        const auto info = new AssemblyInfo();
        info->image = assembly->image;
        info->assembly = assembly;
        info->next = Mono::assemblies;
        Mono::assemblies = info;
    }
}
#pragma endregion

#pragma region [ internal static ] free_assemblies(AssemblyInfo *assemblies)
void free_assemblies(AssemblyInfo*& assemblies)
{
    if (assemblies != nullptr)
    {
        AssemblyInfo* next;
        for (AssemblyInfo* info = assemblies; info; info = next)
        {
            next = info->next;
            free(info);
        }
        assemblies = nullptr;
    }
}
#pragma endregion

#pragma region Mono::GetAssemblies()
AssemblyInfo* Mono::GetAssemblies()
{
    free_assemblies(assemblies);

    if (mono_assembly_foreach)
    {
        mono_assembly_foreach((MonoFunc)func_assembly_foreach, NULL);
    }

    return assemblies;
}
#pragma endregion

#pragma region Mono::HasAssembly(const char* name, gboolean ref_only)
bool Mono::HasAssembly(const char* name, gboolean ref_only)
{
    for (AssemblyInfo* info = GetAssemblies(); info; info = info->next)
    {
        if (info && info->assembly && strcmp(info->image->assembly_name, name) == 0 && info->assembly->ref_only ==
            ref_only)
        {
            return true;
        }
    }
    return false;
}
#pragma endregion

#pragma region Mono::GetAssembly(const char* name, gboolean ref_only)
MonoAssembly* Mono::GetAssembly(const char* name, gboolean ref_only)
{
    for (AssemblyInfo* info = GetAssemblies(); info; info = info->next)
    {
        if (info && info->assembly && strcmp(info->image->assembly_name, name) == 0 && info->assembly->ref_only ==
            ref_only)
        {
            return info->assembly;
        }
    }
    return NULL;
}
#pragma endregion

// Load Assembly

#pragma region Mono::LoadAssembly(MonoImage* image, const char* path, MonoImageOpenStatus *status, gboolean ref_only)
MonoAssembly* Mono::LoadAssembly(MonoImage* image, const char* path, MonoImageOpenStatus* status, gboolean ref_only)
{
    if (image)
    {
        MonoAssembly* assembly;

        if (!status) status = &def_status;
        else *status = MONO_IMAGE_OK;

        assembly = mono_assembly_load_from_full(image, image->name, status, ref_only);

        if (assembly && *status == MONO_IMAGE_OK)
        {
            unregister_image(image);
            mono_image_close(image);

            return assembly;
        }
    }

    if (status) *status = MONO_IMAGE_IMAGE_INVALID;
    return NULL;
}
#pragma endregion

#pragma region Mono::LoadAssembly(const char* filepath, MonoImageOpenStatus *status, gboolean ref_only, gboolean protection)
MonoAssembly* Mono::LoadAssembly(const char* filepath, MonoImageOpenStatus* status, gboolean ref_only,
                                 gboolean protection)
{
    if (!status) status = &def_status;
    else *status = MONO_IMAGE_OK;

    char* buffer = NULL;
    size_t length = file_readallbytes(filepath, buffer);

    if (length > 0)
    {
        MonoImage* image = Mono::OpenImage(buffer, length, status, ref_only, protection, filepath);

        if (image && *status == MONO_IMAGE_OK)
        {
            MonoAssembly* assembly = Mono::LoadAssembly(image, filepath, status, ref_only);

            if (assembly && *status == MONO_IMAGE_OK)
            {
                return assembly;
            }
        }
    }

    return NULL;
}
#pragma endregion

#pragma region Mono::LoadAssembly(char* buffer, uint32_t length, MonoImageOpenStatus* status, gboolean ref_only, gboolean protection, const char* name)
MonoAssembly* Mono::LoadAssembly(char* buffer, uint32_t length, MonoImageOpenStatus* status, gboolean ref_only,
                                 gboolean protection, const char* name)
{
    if (!status) status = &def_status;
    else *status = MONO_IMAGE_OK;

    MonoImage* monoImage = Mono::OpenImage(buffer, length, status, ref_only, protection, name);

    if (monoImage != NULL && *status == MONO_IMAGE_OK)
    {
        return Mono::LoadAssembly(monoImage, name, status, ref_only);
    }

    return NULL;
}
#pragma endregion

#pragma region Mono::LoadAssembly(const char* name, char** assemblies_path, MonoImageOpenStatus *status, gboolean ref_only, gboolean protection)
MonoAssembly* Mono::LoadAssembly(const char* name, char** assemblies_path, MonoImageOpenStatus* status,
                                 gboolean ref_only, gboolean protection)
{
    if (assemblies_path != NULL)
    {
        if (!status) status = &def_status;
        else *status = MONO_IMAGE_OK;

        while (*assemblies_path)
        {
            char* path = strconcat(*assemblies_path++, PATH_SEPARATOR, name, ".dll", NULL);

            MonoAssembly* assembly = Mono::LoadAssembly(path, ref_only, status);

            if (assembly && *status == MONO_IMAGE_OK)
            {
                return assembly;
            }
        }
    }

    return NULL;
}
#pragma endregion
