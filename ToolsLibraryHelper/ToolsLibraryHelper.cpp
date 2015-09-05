//
// ToolsLibraryHelper.cpp
//
#include "stdafx.h"

template<class T> void Zero(T* pTarget, int cb)
    {
    memset(pTarget, 0, cb);
    }
template<class T> void Zero(T* pTarget)
    {
    Zero(pTarget, sizeof(*pTarget));
    }

void Construct(SP_DEVINFO_DATA& result)
    {
    Zero(&result);
    result.cbSize = sizeof(SP_DEVINFO_DATA);
    }
void Construct(SP_DEVICE_INTERFACE_DATA& result)
    {
    Zero(&result);
    result.cbSize = sizeof(SP_DEVICE_INTERFACE_DATA);
    }
void Construct(SP_DEVICE_INTERFACE_DETAIL_DATA_W& result, int cbAllocated)
    {
    Zero(&result, cbAllocated);
    result.cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA);    // C
    }

extern "C" __declspec(dllexport) void EnumerateUSBDevices(GUID& guidInterfaceClass)
    {
    int err;
    HDEVINFO hDeviceInfoSet = SetupDiGetClassDevsW(&guidInterfaceClass, nullptr, nullptr, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
    if (hDeviceInfoSet != INVALID_HANDLE_VALUE)
        {
        __try
            {
            DWORD cbRequired;

            // Enumerate all those devices
            for (int iDevInfo = 0;; iDevInfo++)
                {
                SP_DEVINFO_DATA devInfoEnum; Construct(devInfoEnum);
                if (SetupDiEnumDeviceInfo(hDeviceInfoSet, iDevInfo, &devInfoEnum))
                    {
                    if (false)
                        {
                        // Retrieve the device instance ID that is associated with the current device information element
                        cbRequired = 2;     // size is arbitrary, but seems to be necessary that it be non-zero (?)
                        LPWSTR pbDeviceInstanceId = static_cast<LPWSTR>(CoTaskMemAlloc(cbRequired));
                        __try 
                            {
                            if (!SetupDiGetDeviceInstanceIdW(hDeviceInfoSet, &devInfoEnum, pbDeviceInstanceId, cbRequired, &cbRequired))
                                {
                                CoTaskMemFree(pbDeviceInstanceId);
                                pbDeviceInstanceId = static_cast<LPWSTR>(CoTaskMemAlloc(cbRequired));
                                if (!SetupDiGetDeviceInstanceIdW(hDeviceInfoSet, &devInfoEnum, pbDeviceInstanceId, cbRequired, &cbRequired))
                                    {
                                    return;
                                    }
                                }
                            }
                        __finally
                            {
                            CoTaskMemFree(pbDeviceInstanceId);
                            }
                        }

                    if (true)
                        {
                        // Enumerate the interfaces of that device information element
                        SP_DEVICE_INTERFACE_DATA deviceInterfaceData; Construct(deviceInterfaceData);
                        for (int iInterface = 0;; iInterface++)
                            {
                            if (SetupDiEnumDeviceInterfaces(hDeviceInfoSet, &devInfoEnum, &guidInterfaceClass, iInterface, &deviceInterfaceData))
                                {
                                // Retrieve the device path of that interface and 
                                SetupDiGetDeviceInterfaceDetailW(hDeviceInfoSet, &deviceInterfaceData, nullptr, 0, &cbRequired, nullptr);
                                SP_DEVICE_INTERFACE_DETAIL_DATA_W* pInterfaceDetail = static_cast<SP_DEVICE_INTERFACE_DETAIL_DATA_W*>(CoTaskMemAlloc(cbRequired));
                                __try
                                    {
                                    Construct(*pInterfaceDetail, cbRequired);
                                    SP_DEVINFO_DATA devInfoDevice; Construct(devInfoDevice);
                                    if (SetupDiGetDeviceInterfaceDetailW(hDeviceInfoSet, &deviceInterfaceData, pInterfaceDetail, cbRequired, &cbRequired, &devInfoDevice))
                                        {
                                        
                                        }
                                    else
                                        err = GetLastError();
                                    }
                                __finally
                                    {
                                    CoTaskMemFree(pInterfaceDetail);
                                    }
                                }
                            else
                                break; // interface enumeration complete
                            }
                        }
                        
                    }
                else
                    break;  // device enumeration complete
                }
            }
        __finally
            {
            // Clean up the device enumeration
            SetupDiDestroyDeviceInfoList(hDeviceInfoSet);
            }
        }
    else
        {
        err = GetLastError();    
        }
    }