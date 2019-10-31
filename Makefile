.PHONY: publish upload

URI := https://infinibattle.infi.nl
APIKEY := <bot_api_key_here>

all: help

help:
	@echo "Infinibattle"
	@echo ""
	@echo "make build - build the bot"
	@echo "make upload - upload the bot that has been built"
	@echo "make publish - build & upload"

publish: build upload

upload:
	@echo Publishing your bot...
	curl --insecure -X POST -Ffile=@./publish/build.zip $(URI)/api/uploadBot/$(APIKEY)

build:
	dotnet publish -c Release -o ./publish
	cd ./publish && zip -r build.zip *
