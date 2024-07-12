/* eslint-disable @typescript-eslint/naming-convention */

import * as fs from "fs";
import * as path from "path";

import { DependencyContainer } from "tsyringe";
import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";

// WTT imports
import { WTTInstanceManager } from "./WTTInstanceManager";

import { CustomHeadService } from "./CustomHeadService";
import { CustomVoiceService } from "./CustomVoiceService";


class YojenkzHeads
implements IPreSptLoadMod, IPostDBLoadMod
{
    private Instance: WTTInstanceManager = new WTTInstanceManager();
    private version: string;
    private modName = "WTT-TheLongLostHeadsOfYojenkz";

    private customHeadService: CustomHeadService = new CustomHeadService();
    private customVoiceService: CustomVoiceService = new CustomVoiceService();

    debug = false;

    // Anything that needs done on preSptLoad, place here.
    public preSptLoad(container: DependencyContainer): void 
    {
    // Initialize the instance manager DO NOTHING ELSE BEFORE THIS
        this.Instance.preSptLoad(container, this.modName);
        this.Instance.debug = this.debug;
        // EVERYTHING AFTER HERE MUST USE THE INSTANCE

        this.getVersionFromJson();
        this.displayCreditBanner();

        this.customHeadService.preSptLoad(this.Instance);

        this.customVoiceService.preSptLoad(this.Instance);
    }

    // Anything that needs done on postDBLoad, place here.
    postDBLoad(container: DependencyContainer): void 
    {
    // Initialize the instance manager DO NOTHING ELSE BEFORE THIS
        this.Instance.postDBLoad(container);
        // EVERYTHING AFTER HERE MUST USE THE INSTANCE
        this.customHeadService.postDBLoad();
        this.customVoiceService.postDBLoad();
        this.Instance.logger.log(
            `[${this.modName}] Database: Loading complete.`,
            LogTextColor.GREEN
        );
    }

    private getVersionFromJson(): void 
    {
        const packageJsonPath = path.join(__dirname, "../package.json");

        fs.readFile(packageJsonPath, "utf-8", (err, data) => 
        {
            if (err) 
            {
                console.error("Error reading file:", err);
                return;
            }

            const jsonData = JSON.parse(data);
            this.version = jsonData.version;
        });
    }

    private displayCreditBanner(): void 
    {
        this.Instance.logger.log(
            `[${this.modName}] ------------------------------------------------------------------------`,
            LogTextColor.GREEN
        );
        this.Instance.logger.log(
            `[${this.modName}] 380 Release build`,
            LogTextColor.GREEN
        );
        this.Instance.logger.log(
            `[${this.modName}] Developers:           GroovypenguinX, Yojenkz`,
            LogTextColor.GREEN
        );
        this.Instance.logger.log(
            `[${this.modName}] Head? Aye lmao?`,
            LogTextColor.GREEN
        );
        this.Instance.logger.log(
            `[${this.modName}] ------------------------------------------------------------------------`,
            LogTextColor.GREEN
        );
    }
}

module.exports = { mod: new YojenkzHeads() };
