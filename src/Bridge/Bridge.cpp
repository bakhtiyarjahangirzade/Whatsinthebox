#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <objbase.h>
#include <new>

// The Shell loads this native factory. All rendering and managed UI remain
// in the separately registered local server; no CLR is loaded by this DLL.
static const CLSID Preview={0x8a04dbb7,0x7a32,0x4922,{0xa9,0x5a,0xc3,0x3f,0xac,0x9e,0x73,0x3b}};
static const CLSID Thumbnail={0x47ccd7b8,0x35f6,0x4835,{0x96,0x5c,0xf4,0x88,0x33,0x1a,0xde,0x93}};
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
