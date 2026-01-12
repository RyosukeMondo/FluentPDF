# Tasks Document

- [x] 1. Create release.yml workflow file
  - File: .github/workflows/release.yml
  - Set up workflow trigger on version tags and manual dispatch
  - Add initial checkout and setup steps
  - Purpose: Establish release workflow foundation
  - _Leverage: .github/workflows/build.yml_
  - _Requirements: 1.1, 1.2, 1.3_
  - _Prompt: Implement the task for spec release-workflow, first run spec-workflow-guide to get the workflow guide then implement the task: Role: DevOps Engineer with GitHub Actions expertise | Task: Create release.yml with tag trigger (v*.*.*), workflow_dispatch input for version, checkout step following requirements 1.1-1.3 | Restrictions: Follow existing workflow patterns from build.yml, use latest actions versions | Success: Workflow triggers on tag push and manual dispatch | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [x] 2. Add x64 build job
  - File: .github/workflows/release.yml (modify)
  - Add job to build FluentPDF.App for x64 platform
  - Use Release configuration
  - Purpose: Build x64 executable for packaging
  - _Leverage: .github/workflows/build.yml_
  - _Requirements: 2.1, 2.2_
  - _Prompt: Implement the task for spec release-workflow, first run spec-workflow-guide to get the workflow guide then implement the task: Role: DevOps Engineer | Task: Add build-x64 job to release.yml that builds FluentPDF.App for x64 in Release configuration following requirements 2.1 and 2.2 | Restrictions: Use windows-latest runner, cache NuGet packages | Success: Job builds x64 successfully, artifacts uploaded | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [x] 3. Add ARM64 build job
  - File: .github/workflows/release.yml (modify)
  - Add job to build FluentPDF.App for ARM64 platform
  - Run in parallel with x64 build
  - Purpose: Build ARM64 executable for Surface Pro X
  - _Leverage: .github/workflows/build.yml_
  - _Requirements: 2.1, 2.2_
  - _Prompt: Implement the task for spec release-workflow, first run spec-workflow-guide to get the workflow guide then implement the task: Role: DevOps Engineer | Task: Add build-arm64 job to release.yml that builds for ARM64 in Release configuration following requirements 2.1 and 2.2 | Restrictions: Run parallel with x64 job, use windows-latest | Success: Job builds ARM64 successfully | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [x] 4. Create package-msix.ps1 script
  - File: tools/package-msix.ps1
  - PowerShell script to create MSIX package from build output
  - Support platform parameter (x64, arm64)
  - Purpose: Encapsulate MSIX packaging logic
  - _Leverage: tools/build-libs.ps1 for script patterns_
  - _Requirements: 2.1_
  - _Prompt: Implement the task for spec release-workflow, first run spec-workflow-guide to get the workflow guide then implement the task: Role: PowerShell Developer with MSIX expertise | Task: Create package-msix.ps1 script that packages build output into MSIX using makeappx.exe following requirement 2.1 | Restrictions: Accept -Platform and -Configuration parameters, use Windows SDK tools | Success: Script creates valid MSIX package from build output | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [x] 5. Add package job with signing
  - File: .github/workflows/release.yml (modify)
  - Add job to create and sign MSIX packages
  - Use GitHub secrets for certificate
  - Purpose: Create signed packages ready for Store
  - _Leverage: tools/package-msix.ps1_
  - _Requirements: 2.3_
  - _Prompt: Implement the task for spec release-workflow, first run spec-workflow-guide to get the workflow guide then implement the task: Role: DevOps Engineer with code signing expertise | Task: Add package job that runs package-msix.ps1 and signs with certificate from secrets following requirement 2.3 | Restrictions: Use signtool.exe, mask certificate password in logs | Success: Signed MSIX packages created as artifacts | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_

- [ ] 6. Add release publishing job
  - File: .github/workflows/release.yml (modify)
  - Add job to create GitHub release with MSIX assets
  - Generate release notes from commits
  - Purpose: Publish release for download and Store submission
  - _Leverage: existing workflow patterns_
  - _Requirements: 3.1, 3.2_
  - _Prompt: Implement the task for spec release-workflow, first run spec-workflow-guide to get the workflow guide then implement the task: Role: DevOps Engineer | Task: Add release job that creates GitHub release, uploads MSIX assets, generates release notes following requirements 3.1 and 3.2 | Restrictions: Use softprops/action-gh-release, include checksums | Success: GitHub release created with downloadable MSIX packages | After implementation: Mark task as in-progress in tasks.md before starting, use log-implementation tool to record what was done, then mark as complete_
