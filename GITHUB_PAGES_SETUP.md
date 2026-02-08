# GitHub Pages Setup Instructions

This repository is configured to automatically deploy the web version to GitHub Pages using GitHub Actions.

## Current Status

The `deploy-web.yml` workflow is configured to automatically deploy the `web-version/` directory to GitHub Pages whenever changes are pushed to the main branch.

## How It Works

The deployment uses GitHub's official Pages actions:
1. `actions/configure-pages@v4` - Automatically configures GitHub Pages settings
2. `actions/upload-pages-artifact@v3` - Packages the web-version files
3. `actions/deploy-pages@v4` - Deploys to GitHub Pages

This approach ensures GitHub Pages is properly configured and activated automatically without manual intervention.

## What Happens When Changes are Pushed

1. The workflow runs automatically when:
   - Files in `web-version/` are modified
   - The `deploy-web.yml` workflow file is modified
2. The workflow validates the web files (HTML, JS, CSS)
3. It packages and deploys the content to GitHub Pages
4. GitHub Pages serves the site at: https://furukawa1020.github.io/takoyakiapp/

No manual configuration in repository settings is required!

## Manually Triggering Deployment

If you need to redeploy without making code changes:

1. Go to Actions tab in GitHub
2. Select "Deploy Web Version to GitHub Pages" workflow
3. Click "Run workflow" button
4. Select the main branch
5. Click "Run workflow"

## Troubleshooting

If you see a 404 error:
- Wait a few minutes for GitHub Pages to build and deploy (first deployment can take 5-10 minutes)
- Check that the workflow ran successfully in the Actions tab
- Verify the deployment shows a green checkmark in the workflow run
- The official GitHub Pages actions automatically configure the repository settings
