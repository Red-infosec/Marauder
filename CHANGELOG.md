## 2019-09-11
* [Fixed](https://github.com/maraudershell/Marauder/pull/20) .NET 4.5 builds (Thanks @n1xbyte!)
* [Updated](https://github.com/maraudershell/Marauder/commit/8663f9cd1c2e2adf9d239516c6e41550de77c366#diff-2955d5257f635de1df53f55a171ca5c7L41) release builds of Marauder to not spawn a window
* [Removed](https://github.com/maraudershell/Marauder/commit/8663f9cd1c2e2adf9d239516c6e41550de77c366#diff-4a01d427fcced500434f0df1cd43d68aR11) Architecture choices, it all just built as AnyCPU anyways
* [Cleaned up](https://github.com/maraudershell/Marauder/commit/8663f9cd1c2e2adf9d239516c6e41550de77c366#diff-dc6c9246d7ed9db9a3acd6b4fd7a77d3) the csproj file and removed Trimming steps (it was breaking stuff)
* [Fixed](https://github.com/maraudershell/Marauder/pull/24/commits/611fb2978a6eb75c93a8c14092fc165941e45bdf) issue with release builds of the HTTP Transport module so that it actually works

## 2019-07-07
* Updated HTTP Transport to comply with new profile stuff

## 2019-06-03
* PR from @s3b4stian to allow Marauder to speak TLS1.1 and TLS1.2
* More logging for the HTTP Transport module
* Added Azure Pipelines CI (It totally doesn't work yet, but its the thought that counts)
