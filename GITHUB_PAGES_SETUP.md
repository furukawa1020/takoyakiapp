# GitHub Pages Setup Instructions

This repository is configured to automatically deploy the web version to GitHub Pages using GitHub Actions.

## Current Status

The `deploy-web.yml` workflow is configured to automatically deploy the `web-version/` directory to GitHub Pages whenever changes are pushed to the main branch.

## What Happens When This PR is Merged

1. The workflow will run automatically because we've modified files in `web-version/`
2. It will create a `gh-pages` branch (if it doesn't exist)
3. It will push the web version files to that branch
4. GitHub Pages should automatically serve the site from the `gh-pages` branch

## Manual Steps (if needed)

If the site is not accessible after the workflow runs successfully, you may need to manually enable GitHub Pages in the repository settings:

1. Go to Repository Settings → Pages
2. Under "Build and deployment" > "Source", select "Deploy from a branch"
3. Under "Branch", select `gh-pages` and `/ (root)` directory
4. Click Save

The site will then be available at: https://furukawa1020.github.io/takoyakiapp/

## Manually Triggering Deployment

If you need to redeploy without making code changes:

1. Go to Actions tab in GitHub
2. Select "Deploy Web Version to GitHub Pages" workflow
3. Click "Run workflow" button
4. Select the main branch
5. Click "Run workflow"

## Troubleshooting

If you see a 404 error:
- Wait a few minutes for GitHub Pages to build and deploy
- Check that the workflow ran successfully in the Actions tab
- Verify the `gh-pages` branch exists and contains the web files
- Ensure GitHub Pages is enabled in repository settings (see "Manual Steps" above)
