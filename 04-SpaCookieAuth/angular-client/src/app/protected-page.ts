import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, input, signal } from '@angular/core';

type ResourceResponse = { message: string; claims?: { type: string; value: string }[] };

// Shows data from a protected API endpoint. The API decides who may see it:
//   200 -> show the data, 401 -> not logged in (handled by authInterceptor), 403 -> logged in but not allowed
@Component({
  selector: 'app-protected-page',
  template: `
    @if (denied()) {
      <p>Access denied: you are logged in, but not allowed to see this.</p>
    } @else if (data(); as data) {
      <h1>{{ title() }}</h1>
      <p>{{ data.message }}</p>

      @if (data.claims) {
        <table border="1">
          @for (claim of data.claims; track claim.type) {
            <tr>
              <td>{{ claim.type }}</td>
              <td>{{ claim.value }}</td>
            </tr>
          }
        </table>
      }
    } @else {
      <p>Loading...</p>
    }
  `,
})
export class ProtectedPage implements OnInit {
  // From the route's data (see app.routes.ts).
  readonly title = input.required<string>();
  readonly url = input.required<string>();

  private http = inject(HttpClient);
  protected data = signal<ResourceResponse | null>(null);
  protected denied = signal(false);

  ngOnInit() {
    this.http.get<ResourceResponse>(this.url()).subscribe({
      next: data => this.data.set(data),
      error: (error: HttpErrorResponse) => this.denied.set(error.status === 403),
    });
  }
}
