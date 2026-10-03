#if UNITY_5_3_OR_NEWER && YOKIFRAME_YOOASSET_SUPPORT && YOKIFRAME_YOOASSET_2_OR_3

using System;
using System.Collections.Generic;
using YooAsset;

namespace YokiFrame.Unity
{
    public sealed partial class YooAssetResourceProvider
    {
        /// <summary>复制并校验探测清单；空清单表示等待后续动态添加 package。</summary>
        private static List<ResourcePackage> CopyReadyPackages(IReadOnlyList<ResourcePackage> packages)
        {
            if (packages == null || packages.Count == 0)
                return new List<ResourcePackage>();

            var ready = new List<ResourcePackage>(packages.Count);
            for (int index = 0; index < packages.Count; index++)
            {
                ResourcePackage package = packages[index];
                if (package == null || ContainsPackage(ready, package.PackageName))
                    continue;
                if (!YooAssetPackageReadiness.IsReady(package))
                {
                    throw new InvalidOperationException(
                        "YooAsset ResourcePackage must be initialized successfully before installing the ResKit provider.");
                }

                ready.Add(package);
            }
            return ready;
        }

        /// <summary>判断探测清单是否已经包含同名 package，避免重复探测。</summary>
        private static bool ContainsPackage(List<ResourcePackage> packages, string packageName)
        {
            for (int index = 0; index < packages.Count; index++)
            {
                if (string.Equals(packages[index].PackageName, packageName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 按显式包名或登记顺序解析本次加载使用的 package。
        /// 显式包未登记时立即失败，不回退到自动探测。
        /// </summary>
        private ResourcePackage ResolvePackage(string path, out string location)
        {
            YooAssetLocation.Split(path, out string packageName, out location);
            if (!string.IsNullOrEmpty(packageName))
                return RequirePackage(packageName);

            for (int index = 0; index < mPackages.Count; index++)
            {
                ResourcePackage candidate = mPackages[index];
                if (YooAssetPackageReadiness.IsReady(candidate) && IsLocationValid(candidate, location))
                    return candidate;
            }

            if (mPackages.Count == 0)
            {
                throw new InvalidOperationException(
                    "No YooAsset ResourcePackage is registered with the ResKit provider.");
            }

            return mPackages[0];
        }

        /// <summary>按当前 YooAsset 主版本调用位置校验 API，隔离 V2 与 V3 的命名差异。</summary>
        private static bool IsLocationValid(ResourcePackage package, string location)
        {
#if YOKIFRAME_YOOASSET_3
            return package.IsLocationValid(location);
#else
            return package.CheckLocationValid(location);
#endif
        }

        /// <summary>在当前动态探测清单中查找显式 package，缺失时给出稳定错误。</summary>
        private ResourcePackage RequirePackage(string packageName)
        {
            ResourcePackage selected = FindPackage(packageName);
            if (selected != null)
                return selected;

            throw new InvalidOperationException(
                "YooAsset package '" + packageName + "' is not registered with the ResKit provider.");
        }

        /// <summary>在当前探测清单中按 Ordinal 名称查找 package。</summary>
        private ResourcePackage FindPackage(string packageName)
        {
            if (mPackages == null)
                return null;

            for (int index = 0; index < mPackages.Count; index++)
            {
                ResourcePackage package = mPackages[index];
                if (string.Equals(package.PackageName, packageName, StringComparison.Ordinal))
                    return package;
            }

            return null;
        }

        /// <summary>追加或替换一个已初始化的 YooAsset package。</summary>
        public void AddPackage(ResourcePackage package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            if (!YooAssetPackageReadiness.IsReady(package))
            {
                throw new InvalidOperationException(
                    "YooAsset ResourcePackage must be initialized successfully before adding it to the ResKit provider.");
            }

            if (mPackages == null)
                mPackages = new List<ResourcePackage>();

            for (int index = 0; index < mPackages.Count; index++)
            {
                if (string.Equals(mPackages[index].PackageName, package.PackageName, StringComparison.Ordinal))
                {
                    mPackages[index] = package;
                    return;
                }
            }

            mPackages.Add(package);
        }
    }
}

#endif
