def notify(status){
	emailext (
		body: '$DEFAULT_CONTENT',
		recipientProviders: [
			[$class: 'CulpritsRecipientProvider'],
			[$class: 'DevelopersRecipientProvider'],
			[$class: 'RequesterRecipientProvider']
		],
		replyTo: '$DEFAULT_REPLYTO',
		subject: '$DEFAULT_SUBJECT',
		to: '$DEFAULT_RECIPIENTS'
	)
}

@NonCPS
def killall_jobs() {
	def jobname = env.JOB_NAME;
	def buildnum = env.BUILD_NUMBER.toInteger();
	def killnums = "";
	def job = Jenkins.instance.getItemByFullName(jobname);
	def fixed_job_name = env.JOB_NAME.replace('%2F','/');
	def split_job_name = env.JOB_NAME.split(/\/{1}/);

	for (build in job.builds) {
		if (!build.isBuilding()) { continue; }
		if (buildnum == build.getNumber().toInteger()) { continue; println "equals"; }
		if (buildnum < build.getNumber().toInteger()) { continue; println "newer"; }

		echo "Kill task = ${build}";

		killnums += "#" + build.getNumber().toInteger() + ", ";

		build.doStop();
	}

	if (killnums != "") {
		discordSend description: "in favor of #${buildnum}, ignore following failed builds for ${killnums}", footer: "", link: env.BUILD_URL, result: "ABORTED", title: "[${split_job_name[0]}] Killing task(s) ${fixed_job_name} ${killnums}", webhookURL: env.GS2EMU_WEBHOOK;
	}
	echo "Done killing"
}

def buildStepDocker() {
	def split_job_name = env.JOB_NAME.split(/\/{1}/);
	def fixed_job_name = split_job_name[1].replace('%2F',' ');

	try {
		checkout scm;

		def buildenv = "";
		def tag = '';
		def EXTRA_VER = '';


		if(env.TAG_NAME) {
			sh(returnStdout: true, script: "echo '```' > RELEASE_DESCRIPTION.txt");
			env.RELEASE_DESCRIPTION = sh(returnStdout: true, script: "git tag -l --format='%(contents)' ${env.TAG_NAME} >> RELEASE_DESCRIPTION.txt");
			sh(returnStdout: true, script: "echo '```' >> RELEASE_DESCRIPTION.txt");
		}

		if (env.BRANCH_NAME.equals('main')) {
			tag = "latest";
		} else {
			tag = "${env.BRANCH_NAME.replace('/','-')}";
		}

		if (env.TAG_NAME) {
			EXTRA_VER = "--build-arg VER_EXTRA=";
		} else if (env.BRANCH_NAME.equals('main')) {
			EXTRA_VER = "--build-arg VER_EXTRA=-beta"
		} else {
			EXTRA_VER = "--build-arg VER_EXTRA=-${tag}";
		}

		docker.withRegistry("https://index.docker.io/v1/", "dockergraal") {
			def release_name = env.JOB_NAME.replace('%2F','/');
			def release_type = ("${release_name}").replace('/','-').replace('GS2Engine-','').replace('main','').replace('dev','');

			def customImage
			stage("Building NuGet Package") {
				customImage = docker.image("mcr.microsoft.com/dotnet/sdk:7.0");
				customImage.pull();
				customImage.inside("-u 0") {
					sh("chmod 777 -R .");
					sh("dotnet pack GS2Engine/GS2Engine.csproj -c Release");
				}
			}

			def archive_date = sh (
				script: 'date +"-%Y%m%d-%H%M"',
				returnStdout: true
			).trim();

			if (env.TAG_NAME) {
				archive_date = '';
			}


			if (true) {
				stage("Archiving artifacts...") {
					customImage.inside("-u 0") {
						withCredentials([string(credentialsId: 'PREAGONAL_GITHUB_TOKEN', variable: 'GITHUB_TOKEN')]) {
							sh("dotnet nuget push -s https://nuget.pkg.github.com/Preagonal/index.json -k ${env.GITHUB_TOKEN} GS2Engine/bin/Release/*.nupkg");
							sh("chmod 777 -R .");
							discordSend description: "Docker Image: ${DOCKER_ROOT}/${DOCKERIMAGE}:${tag}", footer: "", link: env.BUILD_URL, result: currentBuild.currentResult, title: "[${split_job_name[0]}] Artifact Successful: ${fixed_job_name} #${env.BUILD_NUMBER}", webhookURL: env.GS2EMU_WEBHOOK;
						}
					}
				}
			}
		}
	} catch(err) {
		currentBuild.result = 'FAILURE'

		discordSend description: "", footer: "", link: env.BUILD_URL, result: currentBuild.currentResult, title: "[${split_job_name[0]}] Build Failed: ${fixed_job_name} #${env.BUILD_NUMBER}", webhookURL: env.GS2EMU_WEBHOOK

		notify("Build Failed: ${fixed_job_name} #${env.BUILD_NUMBER}")
		throw err
	}
}

node('master') {
	killall_jobs();
	def split_job_name = env.JOB_NAME.split(/\/{1}/);
	def fixed_job_name = split_job_name[1].replace('%2F',' ');
	checkout(scm);

	env.COMMIT_MSG = sh (
		script: 'git log -1 --pretty=%B ${GIT_COMMIT}',
		returnStdout: true
	).trim();

	discordSend description: "${env.COMMIT_MSG}", footer: "", link: env.BUILD_URL, result: currentBuild.currentResult, title: "[${split_job_name[0]}] Build Started: ${fixed_job_name} #${env.BUILD_NUMBER}", webhookURL: env.GS2EMU_WEBHOOK

	if (env.TAG_NAME) {
		sh(returnStdout: true, script: "echo '```' > RELEASE_DESCRIPTION.txt");
		env.RELEASE_DESCRIPTION = sh(returnStdout: true, script: "git tag -l --format='%(contents)' ${env.TAG_NAME} >> RELEASE_DESCRIPTION.txt");
		sh(returnStdout: true, script: "echo '```' >> RELEASE_DESCRIPTION.txt");
	}
	node("linux") {
		buildStepDocker();
	}

	if (env.TAG_NAME) {
		def DESC = sh(returnStdout: true, script: 'cat RELEASE_DESCRIPTION.txt');
		//discordSend description: "${DESC}", customUsername: "OpenGraal", customAvatarUrl: "https://pbs.twimg.com/profile_images/1895028712/13460_106738052711614_100001262603030_51047_4149060_n_400x400.jpg", footer: "OpenGraal Team", link: "https://github.com/xtjoeytx/GServer-v2/releases/tag/${env.TAG_NAME}", result: "SUCCESS", title: "GS2Emu v${env.TAG_NAME}", webhookURL: env.GS2EMU_RELEASE_WEBHOOK;
	}
	sh "rm -rf ./*"
}
