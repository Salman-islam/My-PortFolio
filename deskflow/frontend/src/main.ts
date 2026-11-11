import { bootstrapApplication } from "@angular/platform-browser";
import { provideHttpClient, withInterceptors } from "@angular/common/http";
import { provideRouter } from "@angular/router";
import { AppComponent } from "./app/app.component";
import { WorkspaceComponent } from "./app/workspace.component";
import { authInterceptor } from "./app/api.service";
bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor])),
    provideRouter([
      { path: "", redirectTo: "dashboard", pathMatch: "full" },
      { path: ":page", component: WorkspaceComponent },
      { path: "**", redirectTo: "dashboard" },
    ]),
  ],
}).catch(console.error);
