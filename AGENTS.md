# Vpet project instructions

## Release version policy

The owner requested this policy on 2026-09-29. It applies to the app and installer version in `release.json`.

- Use a patch increment (`0.0.1`, for example `1.7.0` to `1.7.1`) for adjustments, fixes, and tweaks to existing features, tools, or toys.
- Consider a minor increment (`0.1.0`, for example `1.7.0` to `1.8.0`) only when adding new features, tools, or toys.
- Before applying any minor increment, ask the owner to choose between the proposed minor version and the next patch version. Explain which additions motivate the minor increment and wait for an explicit answer. Authorization to implement a feature or publish a release does not by itself authorize a minor increment.
- Complete the authorized implementation and verification so the changes are reviewable before requesting that version choice. Do not change the minor version or publish it while the choice is pending. If the owner chooses a patch, use the patch version even when the update adds features.
- Keep `release.json`, the current changelog heading, and release documentation consistent with the chosen version. See `GITHUB.md` for the publishing workflow.

Recording or editing this policy alone does not require an installer release or a version change.
