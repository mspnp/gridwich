########################################################################################
## Updates:
##
##     2020.07.17 selliott
##          * checkClean Revised to match Gridwich's src/tests subdirectory structure.
##     2020.05.26 selliott
##			* Added checkClean function to locate orphaned code directories which normal
##            "dotnet clean" can't find due to the way git handles file movements after
##  		  refactoring.  See checkClean -h
##
########################################################################################
## Check for "Dead" code directories.
##
## With Git's mv processing (to move an existing file to a different location in the tree),
## switching between branches leads to directories remaining in the tree on disk that are
## part of the previous branch, but not the current one.  Most often, these directories are
## either empty or retain the bin, obj and TestResults subdirectory trees from a build of
## the previous branch.
##
## This (leaving the "stale" directories) is documented behavior of the git mv command.
## See https://git-scm.com/docs/git-mv
##
## This script identifies these obsolete directories, which can be safely deleted.
##
## Note: it does not actually delete the directories, but displays their names.  It
## is intended to be executed when the current directory is Gridwich's src directory.
########################################################################################
function checkClean() {
	unset OPTIND	# needed, lest getopts will only work for the first time through.
	local cmdDel=""
	local slashes='/'

	while getopts 'CLhH' opt; do
  		case ${opt} in
		    C ) # Generate CMD.exe syntax for explicit, forced directory deletes
				cmdDel="rmdir /s /q"
				slashes='\\'
      			;;
		    L ) # Generate bash syntax for explicit, forced directory deletes
				cmdDel="rmdir --ignore-fail-on-non-empty "
				slashes='/'
      			;;
    		h | \? | H ) # help
				echo "Emit a list of Gridwich.* directories that are no longer used."
				echo "i.e., are either empty or no longer contain files outside of any bin, obj and TestResults subdirectories."
				echo ""
				echo "Usage: ${FUNCNAME[0]} [-C | -L] [-h] "
				echo "   -C emit CMD.EXE compatible directory delete command text.  e.g. \"rmdir /s /q Gridwich.xx\""
				echo "   -L emit bash compatible directory delete command text.  e.g. \"rmdir --ignore-fail-on-non-empty Gridwich.xx\""
				echo "   -h show this help"
				echo ""
				echo "Notes:"
				echo " 1. This function does not actually delete any directory trees.  It only finds them."
				echo " 2. The current directory must be the Gridwich top-level src directory."
				echo " 3. If you want to list the tree structure, use ls -la \$(${FUNCNAME[0]})"
				echo ""
				return 2
      			;;
  		esac
	done
	shift $(( OPTIND -1 ))
	unset OPTIND

	# ensure we're running from src (Gridwich-specific)
	if [[ ${PWD} != */src ]]; then
		echo 1>&2 "Script should be executed when pwd is the Gridwich src directory"
		return 3
	fi

	__checkClean ${slashes} ${cmdDel}  # do the actual work
}
########################################################################################
## The actual worker --
##    $1 = the character to map '/' to (e.g. could want '\' for Windows) (never blank)
##    $2..* = the delete command for prefacing each line (could be blank)
########################################################################################
function __checkClean() {
	local slashes="$1"
	shift
	local cmdDel="$*"

	if [[ ${#cmdDel} -gt 0 ]]; then
		# space pad so as not to get leading space in output if nothing in cmdDel
		cmdDel="${cmdDel} "
	fi

	prefix='Gridwich.*'
	# look through each of the Gridwich src and tests subdirectories, looking for any that
	# don't contain anything other than bin, obj or TestResults subdirectories.

	# ... for each src or tests subdirectory...
	for sdir in $(find ./${prefix} -mindepth 1 -maxdepth 1 \( \( -type d -a \( -name 'src' -o -name 'tests' \)  \) \) -printf '%p\n'); do
		echo "${sdir}"		# echo to remember we came here (used later for uniq)
 		# is there any context except for bin/obj/TestResults?
 		find ${sdir} -mindepth 1 -maxdepth 1 \( \
 			\( -type d -a -not -name 'bin' -a -not -name 'obj' -a -not -name 'TestResults' \) -o \
			\( -not -type d \) \
		\) -printf "%h\n"
	done | \
	# remove any directories where we found anything of interest
	sort | uniq -u | \
	# fix slashes and insert the command (if any) at the start of each line
	sed -e "s!/!${slashes}!g" -e "s!^!${cmdDel}!"
}

##########################################################################
## Bash to process output messages for Code Analysis (FXCop/StyleCop)
## into a presentable format.  See help in function below.
##########################################################################
## To Use:
##   1. In your ~/.bashrc, load this file.  e.g., in a VS Code window,
##      it might be:
##         . ~/Documents/Source/Github/Gridwich/infrastructure/devTools/buildhelpers.bash
##      to pull in the function below.  (Note the leading dot)
##   2. Define an alias to do a build.  Since Code Analysis only runs for
##      files actually compiled, a useful commandline is along the lines of:
##          function getca() {
##              dotnet build --no-incremental | \
##              internal-groupCompilerWarnings
##          }
##       making it easy to include any desired options (like light background)
##       in the function.
##
##  This could be a stand-alone script with only trivial wrapping changes if desired,
##  but the intent is, over time, to include multiple functions in this file.  Using
##  the dot referencing as above will have the benefit of pulling in the (eventual) set
##  of functions at once.
##
## Note: by default, this script processes SA1005 warnings (which
## complain about a lack of leading spaces after '//') to look for
## the sequence '//TODO:' in those source file lines and will then
## present the following TODO text in the output rather than just
## the text of the SA1005 message.  This can be disabled with '-t'
##########################################################################

function internal-groupCompilerWarnings {

	local scriptVersion='2020.03.18a'
	local tmpDelim="~"					# temporary (internal) field delimiter
	local divider="================="	# used in filename divider lines in outptu
	local lastFileName=''				# name of file being processed

	# The string below starts the first segment of the pathname of interest.  e.g., if the
	# source file is in
	#       C:\users\FredBloggs\documents\Git\Projects\Gridwich\src\Gridwich.Core\src\Interfaces\x.cs
	# only show
	#       Gridwich.Core/src/Interfaces/x.cs
	# in the output.

	local segmentFilenamePrefix='Gridwich.'	# This string starts the first segment of the pathname of interest.

 	local lineNo=1
	local fileCount=0

	########## Customization Settings (overridable via options) #####################
	local translateToDos=1			# if 0, leave SA1005 messages as-is.
	local colorsOn=1				# set to zero to skip color hilighting controls
	local darkColorTheme=1			# set to zero if running on a light-background terminal.

	unset OPTIND	# needed, lest getopts will only work for the first time through.

	while getopts 'bchtBCHT' opt; do
  		case ${opt} in
		    b ) # Set output for light-colored background
				darkColorTheme=0
      			;;
    		B ) # Set output for dark-colored background
				darkColorTheme=1
				;;
    		c ) # Disable color hilighting
				colorsOn=0
      			;;
    		C ) # Enable color hilighting
				colorsOn=1
      			;;
    		t ) # Disable //TODO: translation
				translateToDos=0
      			;;
    		T ) # Enable //TODO: translation
				translateToDos=1
      			;;
    		h | \? | H ) # help
				echo "Process compiler output to emit a formatted display of Code Analysis messages."
				echo ""
				echo "Usage: ${FUNCNAME[0]} [-b] [-c] [-h] [-t]"
				echo "   -b tune colorizing for light-background terminals"
				echo "   -c do not include ANSI color highlights in output (i.e., just text)"
				echo "   -h show this help"
				echo "   -t disable display of '//TODO:' lines in place of SA1005 code analysis messages."
				echo "      Notice the lack of space between '//' and 'TODO:'.  This what triggers the SA1005."
				echo ""
				echo "Note: the upper-case equivalents of the options (e.g., -C) are also accepted with the reverse"
				echo "meanings of the lower-case equivalent.  So"
				echo "    ${FUNCNAME[0]} -c -C"
				echo "would override back to including color sequences in the output."
				echo ""
				echo "Defaults: process for //TODO: lines with colorized output for dark-background terminals"
				echo "[Version: ${scriptVersion}]"
				return 2
      			;;
  		esac
	done
	shift $(( OPTIND -1 ))
	unset OPTIND

	##########################
	## Miscellaneous
	##########################
	local doNumber=1				# 0 == do not apply sequential number prefix to output messages

	##########################
	## TODO processing
	##########################
	# set translateToDos to zero for leaving
	#    	warning SA1005: Single line comment should begin with a space.
	# as is.  If set to 1, then the code looks at the original source line
	# to see if it is (exactly) starts with
	#   <whitespace>*//${toDoOutputLeader}
	# (e.g., //TODO:) and replaces the warning line with a
	#     ${toDoOutputLeader} <text from source line>
	# e.g.
	#     //TODO: should replace xyz
	# in the source file is output as:
	#	  30. ( 353,  9): TODO: should replace xyz
	# instead of
	#     30. ( 353,  9): warning SA1005: Single line comment should begin with a space.
	#
	# NOTE: the code depends on no space immediately following the '//', which is what elicits
	#       the SA1005 message from StyleCop.  So '// TODO:' won't get noticed.
	#
	local toDoOutputLeader='TODO: '	# start TODO output message lines with this.
	local todoPrefix='//TODO:'		# prefix on source code comments of interest

	##########################
	## Color Highlighting
	##########################
	# REF: https://www.shellhacks.com/bash-colors/

	# default to not including color highlights
	local todoHilight=''
	local fileNameHilight=''
	local resetColor=''

	if [[ ${colorsOn} -gt 0 ]]; then  # colorize
		resetColor='\e[0m'		# resume default color

		if [[ ${darkColorTheme} -gt 0 ]]; then
			# dark theme hilights.
			todoHilight='\e[36m'  		# cyan highlighting of TODO's.
			fileNameHilight='\e[33m'	# yellow filename in divider
		else
			# light background theme
			todoHilight='\e[31m'  		# red highlighting of TODO's.
			fileNameHilight='\e[34m'	# blue filename in divider
		fi
	fi

	########## Process the lines of the compilation output #####################

	while read -r curLine; do

		# We only want to keep compiler output lines containing CA, CS, SA warning messages.
		# Unfortunately, bash built-in pattern matching is somewhat limited (and extglob overhead is big),
		# so a bit of a kludgy vetting sequence
		#
		#  Input lines of interest look like:
		#	  Models\EncodeStatusData.cs(51,25): warning CA1819: Properties should not return arrays [... C.Core.csproj]'
		#  where it's a warning and in the CA or SA series.  For completeness, include CS series, which
		#  originate from the compiler proper.

		local keepLine=0

		if [[ "${curLine}" =~ " warning " ]]; then  # only look into lines containing warnings
			for c in CA SA CS; do
				# keep line if it includes  "9): warning CA1234: "
				if [[ "${curLine}" =~ [[:digit:]]\)\:[[:space:]]warning[[:space:]]${c}[[:digit:]][[:digit:]][[:digit:]][[:digit:]]\:[[:space:]] ]]; then
					keepLine=1	# of interest...
					break
				fi
			done
		fi

		if [[ ${keepLine} -lt 1 ]]; then
			continue	# skip to next input line
		fi

	 	fileName="${curLine%%\(*}"
		fileName="${fileName//\\/\/}"

		pkgPrefix="${segmentFilenamePrefix}"	# of interest in a filename is first path segment starting with this
		pkgName="${curLine##*\[}" 				# strip part before the [
		pkgName="${pkgName//\\/\/}" 			# flip any backslashes to slashes
		pkgName="${pkgName%\/*}" 				# remove last slash and past (csproj part)
		pkgName="${pkgPrefix}${pkgName##*/${pkgPrefix}}"
		# echo $pkgName

		fullFileName="${pkgName}/${fileName}"
		# echo >&2 "fullFileName = ${fullFileName}"

		# pull the file coordinates & pad out the line and column (allowing sorting)
		fileCoords="${curLine#*\(}"
		fileCoords="${fileCoords%%\)*}"
		fileCoordsLine="${fileCoords%%,*}"
		fileCoordsColumn="${fileCoords#*,}"
		fileCoordsPadded=$(printf "(%04d,%03d)" ${fileCoordsLine} ${fileCoordsColumn})
		fileCoordsSpaced=$(printf "(%4d,%3d)"   ${fileCoordsLine} ${fileCoordsColumn})
		#echo "${fileCoordsPadded}"

		msg="${curLine#*: }" 	# keep after the first ': '
		msg="${msg% [*}" 		# drop everything after the last opening brace.

		if [[ ${translateToDos} -gt 0 ]]; then
			res="${msg}"
			# Looking for: "warning SA1005: Single line comment should begin with a space."
			if [[ "${curLine}" =~ "warning SA1005:" ]]; then
				# get the original source line (from the .cs file)
				srcLine=$(sed -n "${fileCoordsLine}{p;q}" "${fullFileName}")
				if [[ "${srcLine}" =~ "${todoPrefix}" ]]; then
					# drop until the end of the TODO marker
					tmp="${srcLine#*${todoPrefix}}" # remove prefix
					tmp="${tmp## }"					# leading blanks
					tmp="${tmp%% }"					# trailing blanks
					res="${toDoOutputLeader}${tmp}"
				fi
			fi
			msg="${res}"
		fi

		#echo "${msg}"

		outLine="${fullFileName}${tmpDelim}${fileCoordsPadded}${tmpDelim}${fileCoordsSpaced}${tmpDelim}${msg}"
		echo "${outLine}"
	done | \
	sort | \
	uniq | \
	while IFS="${tmpDelim}" read -r fileName paddedCoords spacedCoords message; do
		# group the lines of output by source file
		if [[ "${fileName}" != "${lastFileName}" ]]; then	# start a new group for a new file
			fileCount=$(( fileCount + 1 ))
			if [[ ${fileCount} == 1 ]]; then
				# Start out in default color (sometimes bash locks in another color after ^c of something colorful)
				# Note, in non-colorizing mode, variable is empty so always safe to do.
				echo -e -n "${resetColor}"
			fi
			echo -e "${divider}${fileNameHilight} ${fileName} [${fileCount}] ${resetColor}${divider}"
			lastFileName="${fileName}"
		fi
		if [[ $doNumber -gt 0 ]]; then	# we're numbering the output lines
			tmpLn=$(printf "%5s " "${lineNo}.")
			echo -n "${tmpLn}"
			lineNo=$(( lineNo + 1 ))
		fi
		# echo "Msg = '${message}'"

		didEmit=0		# haven't yet emitted the output line

		if [[ ${translateToDos} -gt 0 ]]; then
			# process '//TODO:' lines from the StyleCop message complaining about
			# the lack of space after '//' to the first line of the text after the //TODO:
			# in the source file.
			if [[ "${message}" =~ ^${toDoOutputLeader}* ]]; then
				restOfMsg="${message:${#toDoOutputLeader}}"
				# dump the line with highlights, being careful to leave escape
				# sequences in the message portion alone.
				echo -n -e "${spacedCoords}: ${todoHilight}${toDoOutputLeader}${resetColor}"
				echo "${restOfMsg}"
				didEmit=1
			fi
		fi

		if [[ ${didEmit} -lt 1 ]]; then   # just do normal output for the line
			echo "${spacedCoords}: ${message}"
		fi
	done | \
	tee >(if [ $(wc -l) -gt 0 ]; then # only output a trailer line if there were any warnings found
		echo -e "${divider}${divider}${divider}${divider}${divider}"
	fi)
}
################################################################
## Internal routine to build the required dotnet build command
################################################################
function __dotNetRebuildCmd {
	local cmd="dotnet build"

	while getopts 'fp' opt; do
	case ${opt} in
		f ) # Do full rebuild
			cmd="${cmd} --no-incremental"
			;;
		p ) # Build the command, assuming it will be going to a pipe/file, not a terminal
			cmd="${cmd} -consoleLoggerParameters:DisableConsoleColor 2>&1"
			;;
		esac
	done
	shift $(( OPTIND -1 ))
	unset OPTIND
	echo "${cmd} ${*}"
}
#######################################
## Centalized rebuild workhorse
##    Used by dbf alias, etc.
#######################################
function __dbHelper {
	local compileOptsTerm="$1"
	local compileOptsPiped="$2"
	local doAnalysis="$3"
	local analysisOptsTerm="$4"
	local analysisOptsPiped="$5"

	shift 5

	local compileOpts="${compileOptsPiped}"
	local analysisOpts="${analysisOptsPiped}"
	if [ -t 1 ]; then  # sending to terminal, so different options include color
		compileOpts="${compileOptsTerm}"
		analysisOpts="${analysisOptsTerm}"
	fi

	if [ ${doAnalysis} -eq 0 ]; then
		eval "$(__dotNetRebuildCmd ${compileOpts}) ${*}"
	else
		eval "$(__dotNetRebuildCmd ${compileOpts}) ${*}" | internal-groupCompilerWarnings ${analysisOpts}
	fi
}

#######################################
## User aliases for full/partial rebuilds
#######################################
# minimal rebuild
alias dbp='__dbHelper "" "-p" 0 "" "" '
# minimal compile and only output the corresponding partial set of Code Analysis warnings
alias dbpoa='__dbHelper "" "-p" 1 "" "-c" '
# Full re-build (via --no-incremental on compile)
alias dbf='__dbHelper "-f" "-f -p" 0 "" "" '
# full compile and only output the set of Code Analysis warnings
# Note: observed cases where "dotnet.exe" for some reason recompiles less than the full
#       set of code. For situations where you really need to see all messages from all
#       files, use:
#            dotnet clean && dotnet refresh && dbfoa
alias dbfoa='__dbHelper "-f" "-f -p" 1 "" "-c" '
