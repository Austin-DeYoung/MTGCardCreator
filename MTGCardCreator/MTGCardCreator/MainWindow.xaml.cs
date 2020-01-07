using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.ML;

namespace MTGCardCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Training and Testing data file locations must be updated with the current file locations
        static readonly string trainingData = @"C:\Users\austi\Documents\Ferris\MachineLearning\MachineLearning\FinalProject\MTGCardCreator\TrainingData.csv";
        static readonly string testData = @"C:\Users\austi\Documents\Ferris\MachineLearning\MachineLearning\FinalProject\MTGCardCreator\TestingData.csv";

        List<TextBox> textBoxes = new List<TextBox>();

        //MLContext mlContext = new MLContext(seed: 0);
        MLContext mlContext = new MLContext(seed: 0);
        ITransformer model;

        public MainWindow()
        {
            InitializeComponent();

            textBoxes.Add(txtConvertedManaCost);
            textBoxes.Add(txtWhite);
            textBoxes.Add(txtBlue);
            textBoxes.Add(txtBlack);
            textBoxes.Add(txtRed);
            textBoxes.Add(txtGreen);
            textBoxes.Add(txtPower);
            textBoxes.Add(txtToughness);
            textBoxes.Add(txtLoyalty);

            //Thread used to generate model while GUI is running
            ThreadStart learningThread = new ThreadStart(MachineLearning);
            Thread machineLearningThread = new Thread(learningThread);
            machineLearningThread.Start();


        }

        public void MachineLearning()
        {
            model = Train(mlContext, trainingData);
        }

        #region MachineLearningAlgorithmMethods

        public static ITransformer Train(MLContext mlContext, string dataPath)
        {
            //Loads data from text file
            IDataView dataView = mlContext.Data.LoadFromTextFile<CardData>(dataPath, hasHeader: true, separatorChar: ',');

            //Regressions algorithm expects numeric features so this transforms the categorical data into numbers and then combines all
            //features into a single column which can be used by the regression algorithm
            var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "Price")
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "ArtistEncoded", inputColumnName: "Artist"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "LoyaltyEncoded", inputColumnName: "Loyalty"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "ManaCostEncoded", inputColumnName: "ManaCost"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "NameEncoded", inputColumnName: "Name"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "PowerEncoded", inputColumnName: "Power"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "RarityEncoded", inputColumnName: "Rarity"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "TextEncoded", inputColumnName: "Text"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "ToughnessEncoded", inputColumnName: "Toughness"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "TypeEncoded", inputColumnName: "Type"))
                .Append(mlContext.Transforms.Concatenate("Features", "ArtistEncoded", "ConvertedManaCost", "LoyaltyEncoded", "ManaCostEncoded", "NameEncoded", "PowerEncoded", "RarityEncoded", "TextEncoded", "ToughnessEncoded", "TypeEncoded"))
                .Append(mlContext.Regression.Trainers.Sdca());
            //Attempted to use FastTree method, but recieved errors

            var model = pipeline.Fit(dataView);
            return model;
        }

        private static void Evaluate(MLContext mlContext, ITransformer model)
        {
            //Not called in this application but can be used to evaluate against test data to find RSquared and Root Mean Squared Error
            IDataView dataView = mlContext.Data.LoadFromTextFile<CardData>(testData, hasHeader: true, separatorChar: ',');

            var predictions = model.Transform(dataView);

            var metrics = mlContext.Regression.Evaluate(predictions, "Label", "Score");

            Console.WriteLine();
            Console.WriteLine("RSquared Score: {0}", metrics.RSquared);
            Console.WriteLine("Root Mean Squared Error: {0}", metrics.RootMeanSquaredError);

        }

        private static double TestSinglePredition(MLContext mLContext, ITransformer model, float convertedManaCost, 
            string loyalty, string manaCost, string power, string rarity, string toughness, string type)
        {
            //This method takes in all of the user's input and creates an instance of CardData
            //This instance of CardData is then used to predict a price for the user's created card

            var predictFunction = mLContext.Model.CreatePredictionEngine<CardData, PricePrediction>(model);

            var createdCard = new CardData()
            {
                Artist = "",
                ConvertedManaCost = convertedManaCost,
                Loyalty = loyalty,
                ManaCost = manaCost,
                Name = "",
                Power = power,
                Rarity = rarity,
                Text = "",
                Toughness = toughness,
                Type = type,
                //Price = 0 //Actual = 0.24
            };

            var prediction = predictFunction.Predict(createdCard);
            return Math.Round(Convert.ToDouble(prediction.Price), 2);
            //Console.WriteLine("Predicted Price: {0}, Actual Price: 0.24", prediction.Price);
        }
        #endregion

        private void FirstListBox_Selected(object sender, RoutedEventArgs e)
        {
            string selectedItem = ((ListBoxItem)sender).Content.ToString();
            //Refreshes page when a new type is selected and enables situation fields if needed

            txtPrice.Text = "";

            foreach (var box in textBoxes)
            {
                box.Text = "0";
            }

            if (selectedItem == "Creature")
            {
                txtPower.IsReadOnly = false;
                txtToughness.IsReadOnly = false;
            }
            else
            {
                txtPower.IsReadOnly = true;
                txtToughness.IsReadOnly = true;
                txtPower.Text = "";
                txtToughness.Text = "";
            }

            if(selectedItem == "Planeswalker")
            {
                txtLoyalty.IsReadOnly = false;
                
            }
            else
            {
                txtLoyalty.IsReadOnly = true;
                txtLoyalty.Text = "";
            }
        }

        private void BtnPrice_Click(object sender, RoutedEventArgs e)
        {
            
            //All Needed Variables
            #region Variables
            bool manaIsCorrect = true;
            List<int> manaColors = new List<int>();
            int convertedManaCost = Convert.ToInt32(txtConvertedManaCost.Text);
            int whiteMana = Convert.ToInt32(txtWhite.Text);
            int blueMana = Convert.ToInt32(txtBlue.Text);
            int blackMana = Convert.ToInt32(txtBlack.Text);
            int redMana = Convert.ToInt32(txtRed.Text);
            int greenMana = Convert.ToInt32(txtGreen.Text);
            int totalColorMana = 0;


            manaColors.Add(whiteMana);
            manaColors.Add(blueMana);
            manaColors.Add(blackMana);
            manaColors.Add(redMana);
            manaColors.Add(greenMana);

            #endregion

            //Error checking for the user's input
            #region Conditions

            if (convertedManaCost < 0 || convertedManaCost > 12)
            {
                manaIsCorrect = false;
            }

            foreach (var color in manaColors)
            {
                if (color < 0 || color > 3)
                {
                    manaIsCorrect = false;
                }
                else
                {
                    totalColorMana += color;
                }
            }

            if (convertedManaCost < totalColorMana)
            {
                manaIsCorrect = false;
            }

            if (txtPower.IsReadOnly == false)
            {
                int power = Convert.ToInt32(txtPower.Text);
                if (power < 1 || power > 10)
                {
                    manaIsCorrect = false;
                }
            }

            if (txtToughness.IsReadOnly == false)
            {
                int toughness = Convert.ToInt32(txtToughness.Text);
                if (toughness < 1 || toughness > 10)
                {
                    manaIsCorrect = false;
                }
            }

            if (txtLoyalty.IsReadOnly == false)
            {
                int loyalty = Convert.ToInt32(txtLoyalty.Text);
                if (loyalty < 1 || loyalty > 7)
                {
                    manaIsCorrect = false;
                }
            }
            #endregion


            //Creates ManaCost variable
            if (lbTypes.SelectedIndex != -1 && lbRarity.SelectedIndex != -1 && manaIsCorrect)
            {
                
                string manaCost = "{" + (convertedManaCost - totalColorMana) + "}";
                int colorChecker = 1;
                foreach (var color in manaColors)
                {
                    int counter = 0;
                    while (counter < color)
                    {
                        switch (colorChecker)
                        {
                            case 1:
                                manaCost += "{W}";
                                break;
                            case 2:
                                manaCost += "{U}";
                                break;
                            case 3:
                                manaCost += "{B}";
                                break;
                            case 4:
                                manaCost += "{R}";
                                break;
                            case 5:
                                manaCost += "{G}";
                                break;
                            default:
                                break;
                        }


                        counter++;
                    }
                    colorChecker++;
                }


                //Predicts price and outputs it to user
                double predictedPrice = TestSinglePredition(mlContext, model, (float) convertedManaCost, txtLoyalty.Text, manaCost, txtPower.Text, 
                    (lbRarity.SelectedItem as ListBoxItem).Content.ToString().ToLower(), txtToughness.Text, (lbTypes.SelectedItem as ListBoxItem).Content.ToString());

                if (predictedPrice < 0)
                {
                    predictedPrice = 0.00;
                }

                txtPrice.Text = predictedPrice.ToString();
            }










            
        }
    }
}
