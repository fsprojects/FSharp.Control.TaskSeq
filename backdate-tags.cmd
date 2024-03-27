@echo off

REM Batch file to override the date and/or message of existing tag, or create a new
REM tag that takes the same date/time of an existing commit.
REM 
REM Usage:
REM  > backdate-tags.cmd v0.1.1 "New message"
REM
REM How it works:
REM  * checkout the commit at the moment of the tag
REM  * get the date/time of that commit and store in GIT_COMMITER_DATE env var
REM  * recreate the tag (it will now take the date of its commit)
REM  * push tags changes to remove (with --force)
REM  * return to HEAD
REM
REM PS: 
REM  * these escape codes are for underlining the headers so they stand out between all GIT's output garbage
REM  * the back-dating trick is taken from here: https://stackoverflow.com/questions/21738647/change-date-of-git-tag-or-github-release-based-on-it

ECHO.
ECHO [4;97mList existing tags:[0m
git tag -n

ECHO.
ECHO [4;97mCheckout to tag:[0m
git checkout tags/%1

REM Output the first string, containing the date of commit, and put it in a file
REM then set the contents of that file to env var GIT_COMMITTER_DATE (which in turn is needed to enable back-dating)
REM then delete the temp file
ECHO.
ECHO [4;97mRetrieve original commit date[0m

git show --format=%%aD | findstr "^[MTWFS][a-z][a-z],.*" > _date.tmp
< _date.tmp (set /p GIT_COMMITTER_DATE=)
del _date.tmp

ECHO Committer date for tag: %GIT_COMMITTER_DATE%
ECHO Overriding tag '%1' with text: %2
ECHO.
REM Override (with -af) the tag, if it exists (no quotes around %2)
git tag -af %1 -m %2

ECHO.
ECHO [4;97mUpdated tag:[0m
git tag --points-at HEAD -n
ECHO.

REM Push to remove and override (with --force)
ECHO [4;97mPush changes to remote[0m
git push --tags --force

REM Go back to original HEAD
ECHO.
ECHO [4;97mBack to original HEAD[0m
git checkout -

ECHO.
ECHO [4;97mList of all tags[0m
git tag -n
