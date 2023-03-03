Contributing

Please conform to the following guidelines when contributing:

## Development Cycle

We prefer all changes to the app to be discussed beforehand,
either in a GitHub issue, or in a an email thread with primary developers or other contributors.

### Issues

Unless the change is tied to a hot-fix, all pull requests must have an issue related to them.

### Labels

Issues and pull requests must include labels prefixed with "kind" and "priority", all others are optional and can be applied as such.

### Pull Requests

We prefer pull-requests that are descriptive of the changes being made
and highlight any potential benefits/drawbacks of the change, but these
types of write-ups are not required.

Even if your change is really small, please open a pull request about it.

Please note that as this project is under MFDLABS, branch naming rules for this organization will be enforced and are as follows:

> Revised Code Branches, supersedes old branches rules. <br />
> Branches are split like this for branches ***<u>not specific to teams</u>***: <br />
> ***{operation}/{fixture}***
> 
> Branchesare split like this for branches ***<u>specific to teams</u>***: <br />
> ***{operation}/{teamName}/{fixture}***
> 
> The following operations are allowed: <br />
> <hr />
>
> **feature** -> *Represents a single feature to be integrated.* <br />
> **ops** -> *Refers to a major overhaul with a core operation of the system or a dependency change.* <br />
> **fix** -> *Refers to a single bug fix.* <br />
> **enhancement** -> *Refers to changes or enhancements made to already existing repository files.* <br />
> **dev** -> *Use this to refer to an entire branch prefix of releases dedicated to a team (as in it will be merged here before it gets merged to master)* <br />
> **hotfix** -> *Use this when implementing crucial hotfixes into production.* <br />
>
> <hr />
> 
> You ***MUST*** create a new branch for every new change, fix etc. you make, if the changes can be split, do it.
> 
> For the fixture, it can only contain characters *`a-zA-Z1-0\-_`*, try to be brief with the name but also giving information about the change.
>
> If you can, try to also include Jira ticket IDs or backlog issue urls.
> Example: <br />
> PR Title: ***[BCKLOG-657](https://jira.dex2.vmminfra.dev/BCKLOG-657?from=gh)***: *Runtime fixture for possible crash exploit exposure through admin API.* <br />
> Branch: ***[fix/security-ops/bcklog-657-potential-crash-exploit](https://github.vmminfra.dev/mfdlabs/backlog/tree/fix/security-ops/bcklog-657-potential-crash-exploit)***.
> 
> When you merge your PR into your chosen branch, do not delete the branch.
>
> Pull requests must be reviewed by codeowners (if applicable) or a repository owner if you are contributing to them.

### Commits

The main ruling for commits is that they are preferred be signed with a GPG key, if you made the change via github.com, they should be signed as default.

To learn how to do this, please follow this guide:
- [Signing Commits](https://docs.github.com/en/authentication/managing-commit-signature-verification/signing-commits)

### Reviews

When you create a pull-request, even if you're a repository administrator, you will have to go through code review. You can review the [CODEOWNERS](./CODEOWNERS) file for a list of default assigned members that will review your pull-request, or you can request another repository administrator or organization member to do it for you.

## Coding Style

As a general rule, follow the coding style already set in the file you
are editing, or look at a similar file if you are adding a new one.

### Documentation Style

Any new code should always have documentation comments above it, and your code should also be either self documenting or have it's own comments to explain what it does, but that doesn't mean commenting on the most obvious things like variable assignment.

If using GitHub co-pilot, please try to remove any of the comments made to generate the code.
