#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <objbase.h>
#include <new>

// The Shell loads this native factory. All rendering and managed UI remain
// in the separately registered local server; no CLR is loaded by this DLL.
static const CLSID Preview={0xeaac2037,0x3eb7,0x4d93,{0x87,0x16,0x1b,0x6a,0x26,0x9c,0x03,0x58}};
static const CLSID Thumbnail={0x916d5157,0xf38c,0x4068,{0xa7,0xe9,0x61,0x3e,0x8e,0x6d,0xfd,0x64}};
static LONG objects=0;

class Factory final : public IClassFactory {
 LONG references=1;
 CLSID clsid;
public:
 explicit Factory(REFCLSID value):clsid(value){InterlockedIncrement(&objects);}
 ~Factory(){InterlockedDecrement(&objects);}
 HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid,void** value) override {
  if(!value)return E_POINTER;
  *value=nullptr;
  if(iid!=IID_IUnknown&&iid!=IID_IClassFactory)return E_NOINTERFACE;
  *value=static_cast<IClassFactory*>(this);AddRef();return S_OK;
 }
 ULONG STDMETHODCALLTYPE AddRef() override{return InterlockedIncrement(&references);}
 ULONG STDMETHODCALLTYPE Release() override{ULONG remaining=InterlockedDecrement(&references);if(!remaining)delete this;return remaining;}
 HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown* outer,REFIID iid,void** value) override {
  if(!value)return E_POINTER;
  *value=nullptr;
  if(outer)return CLASS_E_NOAGGREGATION;
  return CoCreateInstance(clsid,nullptr,CLSCTX_LOCAL_SERVER,iid,value);
 }
 HRESULT STDMETHODCALLTYPE LockServer(BOOL lock) override{if(lock)InterlockedIncrement(&objects);else InterlockedDecrement(&objects);return S_OK;}
};
HRESULT __stdcall DllGetClassObject(REFCLSID clsid,REFIID iid,void** value){
 if(!value)return E_POINTER;
 *value=nullptr;
 if(clsid!=Preview&&clsid!=Thumbnail)return CLASS_E_CLASSNOTAVAILABLE;
 auto factory=new(std::nothrow) Factory(clsid);
 if(!factory)return E_OUTOFMEMORY;
 HRESULT result=factory->QueryInterface(iid,value);factory->Release();return result;
}
HRESULT __stdcall DllCanUnloadNow(){return objects==0?S_OK:S_FALSE;}
